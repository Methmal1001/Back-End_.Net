using System.Security.Claims;
using System.Text;
using System.Text.Json;
using NZWalks.API.Models.Domain.HR;
using NZWalks.API.Models.DTO.Chatbot;
using NZWalks.API.Repositories;
using NZWalks.API.Repositories.HR;

namespace NZWalks.API.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly ILeaveRepository _leaveRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IPayrollRepository _payrollRepo;
        private readonly IPerformanceRepository _performanceRepo;
        private readonly IActivityLogRepository _activityLogRepo;
        private readonly IProductRepository _productRepo;
        private readonly ILogger<ChatbotService> _logger;

        // Pending file state — scoped service, one per request, safe
        private byte[]? _pendingFileBytes;
        private string? _pendingFileName;
        private string? _pendingContentType;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Gemini returns "args": "" when a function has no parameters — guard against it
        private static readonly JsonElement _emptyArgs = JsonDocument.Parse("{}").RootElement;

        private const string SystemPrompt =
            "You are an intelligent ERP assistant for NZWalks with read-only access to HR data (employees, " +
            "departments, job positions, leave, attendance, payroll, performance reviews), Inventory (products), " +
            "and the system activity log. " +
            "You can only query and report on data — you cannot create, update, or delete any records. " +
            "If the user asks you to perform a write operation (create, update, delete, approve, etc.), " +
            "politely explain that you have read-only access and cannot perform that action. " +
            "For read/query requests, call the appropriate tool immediately and summarise the results clearly " +
            "in plain English. " +
            "If a user asks for a report or export, use the export tool and inform them the file is ready. " +
            "When tool results contain IDs needed for subsequent calls, resolve them automatically without " +
            "asking the user for IDs.";

        public ChatbotService(
            HttpClient http,
            IConfiguration config,
            IEmployeeRepository employeeRepo,
            ILeaveRepository leaveRepo,
            IAttendanceRepository attendanceRepo,
            IPayrollRepository payrollRepo,
            IPerformanceRepository performanceRepo,
            IActivityLogRepository activityLogRepo,
            IProductRepository productRepo,
            ILogger<ChatbotService> logger)
        {
            _http = http;
            _config = config;
            _employeeRepo = employeeRepo;
            _leaveRepo = leaveRepo;
            _attendanceRepo = attendanceRepo;
            _payrollRepo = payrollRepo;
            _performanceRepo = performanceRepo;
            _activityLogRepo = activityLogRepo;
            _productRepo = productRepo;
            _logger = logger;
        }

        // ── Public entry point ────────────────────────────────────────────────

        public async Task<ChatResponse> AskAsync(ChatRequest request, ClaimsPrincipal user)
        {
            var permissions = user.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var userId = user.FindFirst("sub")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var tools = GetAllowedTools(permissions);

            // Build Gemini contents array from history + current message
            var contents = new List<object>();
            foreach (var h in request.History)
                contents.Add(new { role = h.Role, parts = new[] { new { text = h.Content } } });
            contents.Add(new { role = "user", parts = new[] { new { text = request.Message } } });

            string? finalText = null;

            // Agentic loop — up to 5 sequential tool calls per request
            for (int i = 0; i < 5; i++)
            {
                JsonElement geminiResponse;
                try
                {
                    geminiResponse = await CallGeminiAsync(tools, contents);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gemini API call failed");
                    return new ChatResponse { Reply = "I'm having trouble reaching the AI service right now. Please try again shortly." };
                }

                if (!geminiResponse.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    finalText = "The AI did not return a usable response.";
                    break;
                }

                var content = candidates[0].GetProperty("content");
                var parts = content.GetProperty("parts");
                var firstPart = parts[0];

                if (firstPart.TryGetProperty("text", out var textEl))
                {
                    finalText = textEl.GetString() ?? string.Empty;
                    break;
                }

                if (firstPart.TryGetProperty("functionCall", out var funcCallEl))
                {
                    var funcName = funcCallEl.GetProperty("name").GetString()!;
                    var funcArgs = funcCallEl.GetProperty("args");

                    // Append model's original content as-is — newer Gemini models require thoughtSignature
                    // to be forwarded verbatim; reconstructing the part would drop it and cause a 400.
                    contents.Add(content);

                    object toolResult;
                    try
                    {
                        toolResult = await DispatchToolAsync(funcName, funcArgs, userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Tool {Tool} threw an exception", funcName);
                        toolResult = new { error = ex.Message };
                    }

                    // Append function response as a user turn (Gemini's protocol)
                    contents.Add(new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { functionResponse = new { name = funcName, response = new { result = toolResult } } }
                        }
                    });
                }
                else
                {
                    finalText = "I wasn't able to produce a response for that request.";
                    break;
                }
            }

            finalText ??= "I wasn't able to complete that in the allowed number of steps. Please try rephrasing.";

            if (_pendingFileBytes != null)
            {
                return new ChatResponse
                {
                    Reply = finalText,
                    IsFile = true,
                    FileName = _pendingFileName,
                    ContentType = _pendingContentType,
                    FileBase64 = Convert.ToBase64String(_pendingFileBytes)
                };
            }

            return new ChatResponse { Reply = finalText };
        }

        // ── Gemini API call ───────────────────────────────────────────────────

        private async Task<JsonElement> CallGeminiAsync(List<object> tools, List<object> contents)
        {
            var apiKey = _config.GetSection("GeminiSettings")["ApiKey"]!;
            var model = _config.GetSection("GeminiSettings")["Model"] ?? "gemini-1.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var body = new
            {
                systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
                tools = new[] { new { functionDeclarations = tools } },
                contents
            };

            var json = JsonSerializer.Serialize(body, _jsonOpts);
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var resp = await _http.SendAsync(req);
            var raw = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini returned {Status}: {Body}", resp.StatusCode, raw);
                throw new HttpRequestException($"Gemini API error {resp.StatusCode}");
            }

            return JsonDocument.Parse(raw).RootElement;
        }

        // ── Tool dispatch ─────────────────────────────────────────────────────

        private async Task<object> DispatchToolAsync(string name, JsonElement args, string? userId)
        {
            if (args.ValueKind != JsonValueKind.Object)
                args = _emptyArgs;

            return name switch
            {
                "list_employees"         => await HandleListEmployeesAsync(args),
                "get_employee"           => await HandleGetEmployeeAsync(args),
                "list_departments"       => await HandleListDepartmentsAsync(),
                "list_job_positions"     => await HandleListJobPositionsAsync(args),
                "list_leave_requests"    => await HandleListLeaveRequestsAsync(args),
                "get_leave_balance"      => await HandleGetLeaveBalanceAsync(args),
                "list_attendance"        => await HandleListAttendanceAsync(args),
                "list_payroll"           => await HandleListPayrollAsync(args),
                "list_performance_reviews" => await HandleListPerformanceAsync(args),
                "search_activity_logs"   => await HandleSearchActivityLogsAsync(args),
                "list_products"          => await HandleListProductsAsync(args),
                "export_employees_csv"   => await HandleExportEmployeesCsvAsync(args),
                "export_payroll_csv"     => await HandleExportPayrollCsvAsync(args),
                _                        => new { error = $"Unknown tool: {name}" }
            };
        }

        // ── Read tool handlers ────────────────────────────────────────────────

        private async Task<object> HandleListEmployeesAsync(JsonElement args)
        {
            var search = Str(args, "search");
            var status = Str(args, "status");
            var pageSize = Math.Min(Int(args, "pageSize", 20), 50);
            var (items, total) = await _employeeRepo.GetAllAsync(search, null, status, "FirstName", false, 1, pageSize);
            return new
            {
                totalCount = total,
                shown = items.Count,
                employees = items.Select(e => new
                {
                    e.Id, e.EmployeeNo, fullName = $"{e.FirstName} {e.LastName}",
                    e.Email, e.Status, e.BasicSalary, department = e.DepartmentId
                })
            };
        }

        private async Task<object> HandleGetEmployeeAsync(JsonElement args)
        {
            var id = Guid(args, "id");
            if (id.HasValue)
            {
                var e = await _employeeRepo.GetByIdAsync(id.Value);
                if (e == null) return new { error = "Employee not found." };
                return FormatEmployee(e);
            }
            var name = Str(args, "name") ?? string.Empty;
            var (items, _) = await _employeeRepo.GetAllAsync(name, null, null, "FirstName", false, 1, 5);
            if (items.Count == 0) return new { error = "No employee found matching that name." };
            if (items.Count == 1) return FormatEmployee(items[0]);
            return new { multipleMatches = true, employees = items.Select(e => new { e.Id, fullName = $"{e.FirstName} {e.LastName}", e.Email }) };
        }

        private async Task<object> HandleListDepartmentsAsync()
        {
            var depts = await _employeeRepo.GetAllDepartmentsAsync();
            return new { count = depts.Count, departments = depts.Select(d => new { d.Id, d.Name, d.Code, d.IsActive }) };
        }

        private async Task<object> HandleListJobPositionsAsync(JsonElement args)
        {
            var deptId = Guid(args, "departmentId");
            var positions = await _employeeRepo.GetAllJobPositionsAsync(deptId);
            return new { count = positions.Count, positions = positions.Select(p => new { p.Id, p.Title, p.DepartmentId, p.Level, p.MinSalary, p.MaxSalary, p.IsActive }) };
        }

        private async Task<object> HandleListLeaveRequestsAsync(JsonElement args)
        {
            var employeeId = Guid(args, "employeeId");
            var status = Str(args, "status");
            var requests = await _leaveRepo.GetLeaveRequestsAsync(employeeId, status);
            return new
            {
                count = requests.Count,
                leaveRequests = requests.Take(20).Select(r => new
                {
                    r.Id, r.EmployeeId, r.StartDate, r.EndDate,
                    r.TotalDays, r.Status, r.Reason, r.ApprovalNote
                })
            };
        }

        private async Task<object> HandleGetLeaveBalanceAsync(JsonElement args)
        {
            var employeeId = Guid(args, "employeeId");
            if (!employeeId.HasValue)
            {
                var name = Str(args, "employeeName") ?? string.Empty;
                var (found, _) = await _employeeRepo.GetAllAsync(name, null, null, "FirstName", false, 1, 1);
                if (found.Count == 0) return new { error = "Employee not found." };
                employeeId = found[0].Id;
            }
            var year = Int(args, "year", DateTime.UtcNow.Year);
            var balances = await _leaveRepo.GetLeaveBalancesByEmployeeAsync(employeeId.Value, year);
            return new { employeeId, year, balances = balances.Select(b => new { b.LeaveTypeId, b.TotalDays, b.UsedDays, b.RemainingDays }) };
        }

        private async Task<object> HandleListAttendanceAsync(JsonElement args)
        {
            var employeeId = Guid(args, "employeeId");
            var from = DT(args, "from");
            var to = DT(args, "to");
            var records = await _attendanceRepo.GetAttendancesAsync(employeeId, from, to);
            return new
            {
                count = records.Count,
                records = records.Take(30).Select(a => new
                {
                    a.EmployeeId, a.Date, a.Status, a.CheckInTime, a.CheckOutTime, a.WorkedHours
                })
            };
        }

        private async Task<object> HandleListPayrollAsync(JsonElement args)
        {
            var employeeId = Guid(args, "employeeId");
            var month = NullableInt(args, "month");
            var year = NullableInt(args, "year");
            var payrolls = await _payrollRepo.GetPayrollsAsync(employeeId, month, year);
            return new
            {
                count = payrolls.Count,
                payrolls = payrolls.Take(20).Select(p => new
                {
                    p.Id, p.EmployeeId, p.Month, p.Year, p.BasicSalary,
                    p.NetSalary, p.Status, p.PaidAt
                })
            };
        }

        private async Task<object> HandleListPerformanceAsync(JsonElement args)
        {
            var employeeId = Guid(args, "employeeId");
            var period = Str(args, "period");
            var reviews = await _performanceRepo.GetReviewsAsync(employeeId, period);
            return new
            {
                count = reviews.Count,
                reviews = reviews.Take(20).Select(r => new
                {
                    r.Id, r.EmployeeId, r.ReviewDate, r.Period,
                    r.Rating, r.Status, r.Strengths, r.Improvements
                })
            };
        }

        private async Task<object> HandleSearchActivityLogsAsync(JsonElement args)
        {
            var userId = Guid(args, "userId");
            var module = Str(args, "module");
            var action = Str(args, "action");
            var isSuccess = Bool(args, "isSuccess");
            var from = DT(args, "from");
            var to = DT(args, "to");
            var pageSize = Math.Min(Int(args, "pageSize", 20), 50);
            var (logs, total) = await _activityLogRepo.GetAllAsync(userId, module, action, null, isSuccess, from, to, 1, pageSize);
            return new
            {
                totalCount = total,
                shown = logs.Count,
                logs = logs.Select(l => new
                {
                    l.Id, l.UserName, l.Module, l.Action,
                    l.Summary, l.StatusCode, l.IsSuccess, l.CreatedAt
                })
            };
        }

        private async Task<object> HandleListProductsAsync(JsonElement args)
        {
            var search = Str(args, "search");
            var pageSize = Math.Min(Int(args, "pageSize", 20), 50);
            var (items, total) = await _productRepo.GetAllAsync(search, null, null, null, false, 1, pageSize);
            return new
            {
                totalCount = total,
                shown = items.Count,
                products = items.Select(p => new { p.Id, p.Sku, p.Name, p.UnitPrice, p.IsActive })
            };
        }

        // ── CSV export handlers ───────────────────────────────────────────────

        private async Task<object> HandleExportEmployeesCsvAsync(JsonElement args)
        {
            var status = Str(args, "status");
            var (items, total) = await _employeeRepo.GetAllAsync(null, null, status, "FirstName", false, 1, 1000);

            var sb = new StringBuilder();
            sb.AppendLine("EmployeeNo,FirstName,LastName,Email,Phone,Gender,Department,JobPosition,EmploymentType,Status,BasicSalary,JoiningDate");
            foreach (var e in items)
            {
                sb.AppendLine(string.Join(",",
                    CsvEsc(e.EmployeeNo), CsvEsc(e.FirstName), CsvEsc(e.LastName),
                    CsvEsc(e.Email), CsvEsc(e.Phone), CsvEsc(e.Gender),
                    e.DepartmentId, e.JobPositionId,
                    CsvEsc(e.EmploymentType), CsvEsc(e.Status),
                    e.BasicSalary, e.JoiningDate.ToString("yyyy-MM-dd")));
            }

            _pendingFileBytes = Encoding.UTF8.GetBytes(sb.ToString());
            _pendingFileName = $"employees_{DateTime.UtcNow:yyyyMMdd}.csv";
            _pendingContentType = "text/csv";

            return new { success = true, recordCount = total, fileName = _pendingFileName };
        }

        private async Task<object> HandleExportPayrollCsvAsync(JsonElement args)
        {
            var employeeId = Guid(args, "employeeId");
            var month = NullableInt(args, "month");
            var year = NullableInt(args, "year");
            var payrolls = await _payrollRepo.GetPayrollsAsync(employeeId, month, year);

            var sb = new StringBuilder();
            sb.AppendLine("EmployeeId,Month,Year,BasicSalary,Allowances,Bonuses,OvertimePay,Deductions,TaxAmount,NetSalary,WorkingDays,PresentDays,LeaveDays,Status,PaidAt");
            foreach (var p in payrolls)
            {
                sb.AppendLine(string.Join(",",
                    p.EmployeeId, p.Month, p.Year,
                    p.BasicSalary, p.Allowances, p.Bonuses, p.OvertimePay,
                    p.Deductions, p.TaxAmount, p.NetSalary,
                    p.WorkingDays, p.PresentDays, p.LeaveDays,
                    CsvEsc(p.Status), p.PaidAt?.ToString("yyyy-MM-dd") ?? ""));
            }

            _pendingFileBytes = Encoding.UTF8.GetBytes(sb.ToString());
            _pendingFileName = $"payroll_{DateTime.UtcNow:yyyyMMdd}.csv";
            _pendingContentType = "text/csv";

            return new { success = true, recordCount = payrolls.Count, fileName = _pendingFileName };
        }

        // ── Tool catalogue (permission-filtered) ──────────────────────────────

        private static List<object> GetAllowedTools(HashSet<string> permissions)
        {
            bool Has(string p) => permissions.Contains(p);

            var tools = new List<object>
            {
                Tool("list_employees",
                    "Search and filter employees. Returns a summary list.",
                    Params(
                        P("search", "STRING", "Name or email search keyword"),
                        P("status", "STRING", "Filter by status: Active or Terminated"),
                        P("pageSize", "INTEGER", "Number of results to return (max 50)"))),

                Tool("get_employee",
                    "Get details of a specific employee by their ID (GUID string) or by searching their name.",
                    Params(
                        P("id", "STRING", "Employee GUID — use if you know the exact ID"),
                        P("name", "STRING", "Name to search for — use when you only know the name"))),

                Tool("list_departments",
                    "List all departments in the organisation."),

                Tool("list_job_positions",
                    "List job positions, optionally filtered by department.",
                    Params(
                        P("departmentId", "STRING", "Department GUID to filter by"))),

                Tool("list_products",
                    "Search the product/inventory catalogue.",
                    Params(
                        P("search", "STRING", "Product name or SKU keyword"),
                        P("pageSize", "INTEGER", "Number of results (max 50)"))),

                Tool("export_employees_csv",
                    "Export the full employee list as a downloadable CSV file.",
                    Params(
                        P("status", "STRING", "Optional status filter: Active or Terminated")))
            };

            if (Has("HR.ViewLeave"))
            {
                tools.Add(Tool("list_leave_requests",
                    "Retrieve leave requests, optionally filtered by employee or status.",
                    Params(
                        P("employeeId", "STRING", "Employee GUID to filter"),
                        P("status", "STRING", "Status filter: Pending, Approved, Rejected"))));

                tools.Add(Tool("get_leave_balance",
                    "Get an employee's leave balance for a given year.",
                    Params(
                        P("employeeId", "STRING", "Employee GUID"),
                        P("employeeName", "STRING", "Employee name — use if ID is unknown"),
                        P("year", "INTEGER", "Year (defaults to current year)"))));
            }

            if (Has("HR.ViewAttendance"))
            {
                tools.Add(Tool("list_attendance",
                    "Get attendance records for an employee over a date range.",
                    Params(
                        P("employeeId", "STRING", "Employee GUID"),
                        P("from", "STRING", "Start date (ISO format)"),
                        P("to", "STRING", "End date (ISO format)"))));
            }

            if (Has("HR.ViewPayroll"))
            {
                tools.Add(Tool("list_payroll",
                    "Retrieve payroll records filtered by employee, month, or year.",
                    Params(
                        P("employeeId", "STRING", "Employee GUID"),
                        P("month", "INTEGER", "Month number (1–12)"),
                        P("year", "INTEGER", "Year"))));

                tools.Add(Tool("export_payroll_csv",
                    "Export payroll records as a downloadable CSV file.",
                    Params(
                        P("employeeId", "STRING", "Optional employee GUID to filter"),
                        P("month", "INTEGER", "Optional month filter"),
                        P("year", "INTEGER", "Optional year filter"))));
            }

            if (Has("HR.ViewPerformance"))
            {
                tools.Add(Tool("list_performance_reviews",
                    "Get performance reviews for an employee.",
                    Params(
                        P("employeeId", "STRING", "Employee GUID"),
                        P("period", "STRING", "Review period string e.g. 'Q1-2026'"))));
            }

            if (Has("System.ViewActivityLogs"))
            {
                tools.Add(Tool("search_activity_logs",
                    "Search the global activity/audit log for system actions.",
                    Params(
                        P("module", "STRING", "Module name e.g. Employees, Auth"),
                        P("action", "STRING", "Action name e.g. CreateEmployee"),
                        P("isSuccess", "BOOLEAN", "Filter by success/failure"),
                        P("from", "STRING", "Start date (ISO format)"),
                        P("to", "STRING", "End date (ISO format)"),
                        P("pageSize", "INTEGER", "Results per page (max 50)"))));
            }

            return tools;
        }

        // ── Tool builder helpers ──────────────────────────────────────────────

        private static object Tool(string name, string description, object? parameters = null) =>
            parameters == null
                ? (object)new { name, description }
                : new { name, description, parameters };

        private static object Params(params (string name, string type, string desc)[] props) => new
        {
            type = "OBJECT",
            properties = props.ToDictionary(
                p => p.name,
                p => (object)new { type = p.type, description = p.desc })
        };

        private static (string, string, string) P(string name, string type, string desc) => (name, type, desc);

        // ── JSON arg parsing helpers ──────────────────────────────────────────

        private static string? Str(JsonElement args, string key) =>
            args.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() : null;

        private static int Int(JsonElement args, string key, int def = 0) =>
            args.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number
                ? v.GetInt32() : def;

        private static int? NullableInt(JsonElement args, string key) =>
            args.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number
                ? v.GetInt32() : null;

        private static System.Guid? Guid(JsonElement args, string key) =>
            Str(args, key) is string s && System.Guid.TryParse(s, out var g) ? g : null;

        private static bool? Bool(JsonElement args, string key)
        {
            if (!args.TryGetProperty(key, out var v)) return null;
            return v.ValueKind == JsonValueKind.True ? true :
                   v.ValueKind == JsonValueKind.False ? false : null;
        }

        private static DateTime? DT(JsonElement args, string key) =>
            Str(args, key) is string s && DateTime.TryParse(s, out var d) ? d : null;

        private static decimal Dec(JsonElement args, string key, decimal def = 0) =>
            args.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number
                ? v.GetDecimal() : def;

        // ── Misc helpers ──────────────────────────────────────────────────────

        private static object FormatEmployee(Employee e) => new
        {
            e.Id, e.EmployeeNo, e.FirstName, e.LastName,
            e.Email, e.Phone, e.Gender, e.Status,
            e.DepartmentId, e.JobPositionId, e.BasicSalary,
            e.JoiningDate, e.EmploymentType, e.Address, e.City, e.Country
        };

        private static string CsvEsc(string? v)
        {
            if (v == null) return "";
            if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
                return $"\"{v.Replace("\"", "\"\"")}\"";
            return v;
        }
    }
}
