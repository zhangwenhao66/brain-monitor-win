using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic; // Added for List
using System.Net.Sockets; // Added for SocketException
using System.IO; // Added for IOException

namespace BrainMonitor.Services
{
    public class HttpService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string BaseUrl = "http://localhost:3000/api";

        static HttpService()
        {
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public static async Task<T> PostAsync<T>(string endpoint, object data, string? token = null)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{endpoint}");
                request.Content = content;
                
                // 如果提供了token，添加Authorization头
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
                
                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<T>(responseContent);
                }
                else
                {
                    // 尝试解析错误响应
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        if (!string.IsNullOrEmpty(errorResponse?.Message))
                        {
                            throw new System.Net.Http.HttpRequestException(errorResponse.Message);
                        }
                    }
                    catch (JsonException)
                    {
                        // JSON解析失败，尝试其他方法
                    }
                    
                    // 如果JSON解析失败或没有message字段，尝试正则表达式提取
                    try
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(responseContent, @"""message""\s*:\s*""([^""]*)""");
                        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                        {
                            throw new System.Net.Http.HttpRequestException(match.Groups[1].Value);
                        }
                    }
                    catch
                    {
                        // 正则表达式失败，忽略
                    }
                    
                    // 最后的备选方案：显示状态码和响应内容
                    var errorMessage = $"请求失败 (HTTP {response.StatusCode})";
                    if (!string.IsNullOrWhiteSpace(responseContent))
                    {
                        errorMessage += $": {responseContent}";
                    }
                    throw new System.Net.Http.HttpRequestException(errorMessage);
                }
            }
            catch (TaskCanceledException)
            {
                // 超时异常，统一处理为网络连接失败
                throw new System.Net.Http.HttpRequestException("网络连接失败，请检查网络");
            }
            catch (System.Net.Sockets.SocketException)
            {
                // Socket异常，统一处理为网络连接失败
                throw new System.Net.Http.HttpRequestException("网络连接失败，请检查网络");
            }
            catch (System.IO.IOException)
            {
                // IO异常，可能是网络问题，统一处理为网络连接失败
                throw new System.Net.Http.HttpRequestException("网络连接失败，请检查网络");
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                // 检查是否是网络相关的异常
                if (ex.Message.Contains("由于目标计算机积极拒绝") || 
                    ex.Message.Contains("无法连接") ||
                    ex.Message.Contains("localhost:3000") ||
                    ex.Message.Contains("Connection refused") ||
                    ex.Message.Contains("No connection could be made"))
                {
                    throw new System.Net.Http.HttpRequestException("网络连接失败，请检查网络");
                }
                throw;
            }
            catch (Exception ex)
            {
                // 检查是否是网络相关的异常
                if (ex.Message.Contains("由于目标计算机积极拒绝") || 
                    ex.Message.Contains("无法连接") ||
                    ex.Message.Contains("localhost:3000") ||
                    ex.Message.Contains("Connection refused") ||
                    ex.Message.Contains("No connection could be made"))
                {
                    throw new System.Net.Http.HttpRequestException("网络连接失败，请检查网络");
                }
                throw new System.Net.Http.HttpRequestException($"网络请求异常: {ex.Message}");
            }
        }

        public static async Task<T> GetAsync<T>(string endpoint, string? token = null)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}{endpoint}");
                
                // 如果提供了token，添加Authorization头
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
                
                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<T>(responseContent);
                }
                else
                {
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        if (!string.IsNullOrEmpty(errorResponse?.Message))
                        {
                            throw new System.Net.Http.HttpRequestException(errorResponse.Message);
                        }
                    }
                    catch (JsonException)
                    {
                        // JSON解析失败，尝试其他方法
                    }
                    
                    // 如果JSON解析失败或没有message字段，尝试正则表达式提取
                    try
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(responseContent, @"""message""\s*:\s*""([^""]*)""");
                        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                        {
                            throw new System.Net.Http.HttpRequestException(match.Groups[1].Value);
                        }
                    }
                    catch
                    {
                        // 正则表达式失败，忽略
                    }
                    
                    // 最后的备选方案：显示状态码和响应内容
                    var errorMessage = $"请求失败 (HTTP {response.StatusCode})";
                    if (!string.IsNullOrWhiteSpace(responseContent))
                    {
                        errorMessage += $": {responseContent}";
                    }
                    throw new System.Net.Http.HttpRequestException(errorMessage);
                }
            }
            catch (TaskCanceledException)
            {
                // 超时异常，统一处理为网络连接失败
                throw new System.Net.Http.HttpRequestException("网络连接失败，请检查网络");
            }
            catch (System.Net.Sockets.SocketException)
            {
                // Socket异常，统一处理为网络连接失败
                throw new System.Net.Http.HttpRequestException("网络连接失败，请检查网络");
            }
            catch (System.IO.IOException)
            {
                // IO异常，可能是网络问题，统一处理为网络连接失败
                throw new System.Net.Http.HttpRequestException("网络连接失败，请检查网络");
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                // 检查是否是网络相关的异常
                if (ex.Message.Contains("由于目标计算机积极拒绝") || 
                    ex.Message.Contains("无法连接") ||
                    ex.Message.Contains("localhost:3000") ||
                    ex.Message.Contains("Connection refused") ||
                    ex.Message.Contains("No connection could be made"))
                {
                    throw new System.Net.Http.HttpRequestException("网络连接失败，请检查网络");
                }
                throw;
            }
            catch (Exception ex)
            {
                // 检查是否是网络相关的异常
                if (ex.Message.Contains("由于目标计算机积极拒绝") || 
                    ex.Message.Contains("无法连接") ||
                    ex.Message.Contains("localhost:3000") ||
                    ex.Message.Contains("Connection refused") ||
                    ex.Message.Contains("No connection could be made"))
                {
                    throw new System.Net.Http.HttpRequestException("网络连接失败，请检查网络");
                }
                throw new System.Net.Http.HttpRequestException($"网络请求异常: {ex.Message}");
            }
        }
    }

    // 响应模型
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T Data { get; set; } = default!;
    }

    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // 机构登录请求模型
    public class InstitutionLoginRequest
    {
        [JsonProperty("institutionId")]
        public string InstitutionId { get; set; } = string.Empty;
        
        [JsonProperty("password")]
        public string Password { get; set; } = string.Empty;
    }

    // 机构登录响应模型
    public class InstitutionLoginResponse
    {
        [JsonProperty("institutionId")]
        public string InstitutionId { get; set; } = string.Empty;
        
        [JsonProperty("institutionName")]
        public string InstitutionName { get; set; } = string.Empty;
        
        [JsonProperty("institutionDbId")]
        public int InstitutionDbId { get; set; }
    }

    // 医护人员注册请求模型
    public class MedicalStaffRegisterRequest
    {
        [JsonProperty("staffId")]
        public string StaffId { get; set; } = string.Empty;
        
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("account")]
        public string Account { get; set; } = string.Empty;
        
        [JsonProperty("password")]
        public string Password { get; set; } = string.Empty;
        
        [JsonProperty("phone")]
        public string Phone { get; set; } = string.Empty;
        
        [JsonProperty("department")]
        public string Department { get; set; } = string.Empty;
        
        [JsonProperty("position")]
        public string Position { get; set; } = string.Empty;
        
        [JsonProperty("institutionId")]
        public int InstitutionId { get; set; }
    }

    // 医护人员登录请求模型
    public class MedicalStaffLoginRequest
    {
        [JsonProperty("account")]
        public string Account { get; set; } = string.Empty;
        
        [JsonProperty("password")]
        public string Password { get; set; } = string.Empty;
    }

    // 医护人员登录响应模型
    public class MedicalStaffLoginResponse
    {
        [JsonProperty("token")]
        public string Token { get; set; } = string.Empty;
        
        [JsonProperty("user")]
        public MedicalStaffUser User { get; set; } = new MedicalStaffUser();
    }

    // 医护人员用户信息模型
    public class MedicalStaffUser
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("staffId")]
        public string StaffId { get; set; } = string.Empty;
        
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("account")]
        public string Account { get; set; } = string.Empty;
        
        [JsonProperty("phone")]
        public string Phone { get; set; } = string.Empty;
        
        [JsonProperty("department")]
        public string Department { get; set; } = string.Empty;
        
        [JsonProperty("position")]
        public string Position { get; set; } = string.Empty;
        
        [JsonProperty("institutionId")]
        public int InstitutionId { get; set; }
        
        [JsonProperty("institutionCode")]
        public string InstitutionCode { get; set; } = string.Empty;
        
        [JsonProperty("institutionName")]
        public string InstitutionName { get; set; } = string.Empty;
    }

    // 测试历史记录模型
    public class TestHistoryRecord
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("tester_id")]
        public string TesterId { get; set; } = string.Empty;
        
        [JsonProperty("medical_staff_id")]
        public int MedicalStaffId { get; set; }
        
        [JsonProperty("institution_id")]
        public int InstitutionId { get; set; }
        
        [JsonProperty("test_start_time")]
        public DateTime TestStartTime { get; set; }
        
        [JsonProperty("test_status")]
        public string TestStatus { get; set; } = string.Empty;
        
        [JsonProperty("maca_score")]
        public double? MacaScore { get; set; }
        
        [JsonProperty("mmse_score")]
        public double? MmseScore { get; set; }
        
        [JsonProperty("grip_strength")]
        public double? GripStrength { get; set; }
        
        [JsonProperty("ad_risk_value")]
        public double? AdRiskValue { get; set; }
        
        [JsonProperty("brain_age")]
        public double? BrainAge { get; set; }
        
        [JsonProperty("open_eyes_result_id")]
        public int? OpenEyesResultId { get; set; }
        
        [JsonProperty("closed_eyes_result_id")]
        public int? ClosedEyesResultId { get; set; }
        
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [JsonProperty("medical_staff_name")]
        public string MedicalStaffName { get; set; } = string.Empty;
        
        [JsonProperty("institution_name")]
        public string InstitutionName { get; set; } = string.Empty;
    }

    // 获取测试历史请求模型
    public class GetTestHistoryRequest
    {
        [JsonProperty("testerId")]
        public string TesterId { get; set; } = string.Empty;
        
        [JsonProperty("page")]
        public int Page { get; set; } = 1;
        
        [JsonProperty("pageSize")]
        public int PageSize { get; set; } = 20;
    }

    // 获取测试历史响应模型
    public class GetTestHistoryResponse
    {
        [JsonProperty("records")]
        public List<TestHistoryRecord> Records { get; set; } = new List<TestHistoryRecord>();
        
        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }
        
        [JsonProperty("page")]
        public int Page { get; set; }
        
        [JsonProperty("pageSize")]
        public int PageSize { get; set; }
        
        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }
    }

    // 测试者信息模型
    public class TesterInfo
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("tester_id")]
        public string TesterId { get; set; } = string.Empty;
        
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("age")]
        public string Age { get; set; } = string.Empty;
        
        [JsonProperty("gender")]
        public string Gender { get; set; } = string.Empty;
        
        [JsonProperty("phone")]
        public string Phone { get; set; } = string.Empty;
        
        [JsonProperty("medicalStaffId")]
        public int MedicalStaffId { get; set; }
        
        [JsonProperty("institutionId")]
        public int InstitutionId { get; set; }
        
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
        
        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    // 新增测试者请求模型
    public class CreateTesterRequest
    {
        [JsonProperty("testerId")]
        public string TesterId { get; set; } = string.Empty;
        
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("age")]
        public string Age { get; set; } = string.Empty;
        
        [JsonProperty("gender")]
        public string Gender { get; set; } = string.Empty;
        
        [JsonProperty("phone")]
        public string Phone { get; set; } = string.Empty;
        
        [JsonProperty("medicalStaffId")]
        public int MedicalStaffId { get; set; }
        
        [JsonProperty("institutionId")]
        public int InstitutionId { get; set; }
    }

    // 新增测试者响应模型
    public class CreateTesterResponse
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("testerId")]
        public string TesterId { get; set; } = string.Empty;
        
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("age")]
        public string Age { get; set; } = string.Empty;
        
        [JsonProperty("gender")]
        public string Gender { get; set; } = string.Empty;
        
        [JsonProperty("phone")]
        public string Phone { get; set; } = string.Empty;
        
        [JsonProperty("medicalStaffId")]
        public int MedicalStaffId { get; set; }
        
        [JsonProperty("institutionId")]
        public int InstitutionId { get; set; }
        
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    // 获取测试者列表请求模型
    public class GetTestersRequest
    {
        [JsonProperty("medicalStaffId")]
        public int MedicalStaffId { get; set; }
        
        [JsonProperty("institutionId")]
        public int InstitutionId { get; set; }
        
        [JsonProperty("page")]
        public int Page { get; set; } = 1;
        
        [JsonProperty("pageSize")]
        public int PageSize { get; set; } = 20;
    }

    // 获取测试者列表响应模型
    public class GetTestersResponse
    {
        [JsonProperty("testers")]
        public List<TesterInfo> Testers { get; set; } = new List<TesterInfo>();
        
        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }
        
        [JsonProperty("page")]
        public int Page { get; set; }
        
        [JsonProperty("pageSize")]
        public int PageSize { get; set; }
        
        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }
    }
}
