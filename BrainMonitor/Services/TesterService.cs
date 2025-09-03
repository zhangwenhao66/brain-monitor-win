using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BrainMirror.Services;
using BrainMirror.Views;

namespace BrainMirror.Services
{
    public static class TesterService
    {
        /// <summary>
        /// 创建新的测试者
        /// </summary>
        public static async Task<TesterInfo?> CreateTesterAsync(Tester tester, int medicalStaffId, int institutionId)
        {
            try
            {
                var request = new CreateTesterRequest
                {
                    TesterId = tester.ID,
                    Name = tester.Name,
                    Age = tester.Age,
                    Gender = tester.Gender,
                    Phone = tester.Phone,
                    MedicalStaffId = medicalStaffId,
                    InstitutionId = institutionId
                };

                System.Diagnostics.Debug.WriteLine($"发送创建测试者请求: {System.Text.Json.JsonSerializer.Serialize(request)}");

                var response = await HttpService.PostAsync<ApiResponse<CreateTesterResponse>>("/testers", request, GlobalMedicalStaffManager.CurrentToken);
                
                System.Diagnostics.Debug.WriteLine($"收到响应: Success={response.Success}, Data={response.Data != null}");
                
                if (response.Data != null)
                {
                    System.Diagnostics.Debug.WriteLine($"响应数据: {System.Text.Json.JsonSerializer.Serialize(response.Data)}");
                }
                
                if (response.Success && response.Data != null)
                {
                    try
                    {
                        return new TesterInfo
                        {
                            Id = response.Data.Id,
                            TesterId = response.Data.TesterId,
                            Name = response.Data.Name,
                            Age = response.Data.Age,
                            Gender = response.Data.Gender,
                            Phone = response.Data.Phone,
                            MedicalStaffId = response.Data.MedicalStaffId,
                            InstitutionId = response.Data.InstitutionId,
                            CreatedAt = response.Data.CreatedAt,
                            UpdatedAt = response.Data.CreatedAt
                        };
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"创建TesterInfo对象失败: {ex.Message}");
                        return null;
                    }
                }
                
                return null;
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                // 网络连接相关的异常，重新抛出让调用方处理
                throw;
            }
            catch (Exception ex)
            {
                // 其他异常，记录错误日志并返回 null
                System.Diagnostics.Debug.WriteLine($"创建测试者失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取指定医护人员的所有测试者
        /// </summary>
        public static async Task<List<TesterInfo>> GetAllTestersAsync(int medicalStaffId, int institutionId)
        {
            try
            {
                var request = new GetTestersRequest
                {
                    MedicalStaffId = medicalStaffId,
                    InstitutionId = institutionId,
                    Page = 1,
                    PageSize = 1000
                };

                var response = await HttpService.PostAsync<ApiResponse<GetTestersResponse>>("/testers/list", request, GlobalMedicalStaffManager.CurrentToken);
                
                if (response.Success && response.Data != null)
                {
                    return response.Data.Testers;
                }
                
                return new List<TesterInfo>();
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                // 网络连接相关的异常，重新抛出让调用方处理
                throw;
            }
            catch (Exception ex)
            {
                // 其他异常，记录错误日志并返回空列表
                System.Diagnostics.Debug.WriteLine($"获取所有测试者失败: {ex.Message}");
                return new List<TesterInfo>();
            }
        }

        /// <summary>
        /// 检查测试者ID是否已存在
        /// </summary>
        public static async Task<bool> IsTesterIdExistsAsync(string testerId)
        {
            try
            {
                var response = await HttpService.GetAsync<ApiResponse<dynamic>>($"/testers/{testerId}/exists", GlobalMedicalStaffManager.CurrentToken);
                if (response.Success && response.Data != null)
                {
                    // 后端返回格式: { success: true, data: { exists: boolean } }
                    return response.Data.exists == true;
                }
                return false;
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                // 网络连接相关的异常，重新抛出让调用方处理
                throw;
            }
            catch (Exception ex)
            {
                // 其他异常，记录错误日志并返回 false
                System.Diagnostics.Debug.WriteLine($"检查测试者ID是否存在失败: {ex.Message}");
                return false;
            }
        }
    }
}
