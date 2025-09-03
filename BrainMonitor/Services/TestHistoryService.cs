using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BrainMirror.Services;
using BrainMirror.Views;

namespace BrainMirror.Services
{
    public static class TestHistoryService
    {
        /// <summary>
        /// 获取指定测试者的测试历史记录
        /// </summary>
        /// <param name="testerId">测试者ID</param>
        /// <param name="page">页码，从1开始</param>
        /// <param name="pageSize">每页记录数</param>
        /// <returns>测试历史记录列表</returns>
        public static async Task<List<TestHistoryRecord>> GetTestHistoryAsync(string testerId, int page = 1, int pageSize = 20)
        {
            try
            {
                var request = new GetTestHistoryRequest
                {
                    TesterId = testerId,
                    Page = page,
                    PageSize = pageSize
                };

                var response = await HttpService.PostAsync<ApiResponse<GetTestHistoryResponse>>("/test-records/history", request, GlobalMedicalStaffManager.CurrentToken);
                
                if (response.Success && response.Data != null)
                {
                    return response.Data.Records;
                }
                
                return new List<TestHistoryRecord>();
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                // 网络连接相关的异常，重新抛出让调用方处理
                throw;
            }
            catch (Exception ex)
            {
                // 其他异常，记录错误日志并返回空列表
                System.Diagnostics.Debug.WriteLine($"获取测试历史失败: {ex.Message}");
                return new List<TestHistoryRecord>();
            }
        }

        /// <summary>
        /// 获取指定测试者的测试历史记录（分页信息）
        /// </summary>
        /// <param name="testerId">测试者ID</param>
        /// <param name="page">页码，从1开始</param>
        /// <param name="pageSize">每页记录数</param>
        /// <returns>包含分页信息的测试历史响应</returns>
        public static async Task<GetTestHistoryResponse?> GetTestHistoryWithPaginationAsync(string testerId, int page = 1, int pageSize = 20)
        {
            try
            {
                var request = new GetTestHistoryRequest
                {
                    TesterId = testerId,
                    Page = page,
                    PageSize = pageSize
                };

                var response = await HttpService.PostAsync<ApiResponse<GetTestHistoryResponse>>("/test-records/history", request, GlobalMedicalStaffManager.CurrentToken);
                
                if (response.Success && response.Data != null)
                {
                    return response.Data;
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
                System.Diagnostics.Debug.WriteLine($"获取测试历史失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取指定测试者的所有测试历史记录（不分页）
        /// </summary>
        /// <param name="testerId">测试者ID</param>
        /// <returns>所有测试历史记录</returns>
        public static async Task<List<TestHistoryRecord>> GetAllTestHistoryAsync(string testerId)
        {
            try
            {
                var request = new GetTestHistoryRequest
                {
                    TesterId = testerId,
                    Page = 1,
                    PageSize = 1000 // 设置一个较大的值来获取所有记录
                };

                var response = await HttpService.PostAsync<ApiResponse<GetTestHistoryResponse>>("/test-records/history", request, GlobalMedicalStaffManager.CurrentToken);
                
                if (response.Success && response.Data != null)
                {
                    return response.Data.Records;
                }
                
                return new List<TestHistoryRecord>();
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                // 网络连接相关的异常，重新抛出让调用方处理
                throw;
            }
            catch (Exception ex)
            {
                // 其他异常，记录错误日志并返回空列表
                System.Diagnostics.Debug.WriteLine($"获取所有测试历史失败: {ex.Message}");
                return new List<TestHistoryRecord>();
            }
        }

        /// <summary>
        /// 获取指定测试者的测试历史记录数量
        /// </summary>
        /// <param name="testerId">测试者ID</param>
        /// <returns>测试历史记录数量</returns>
        public static async Task<int> GetTestHistoryCountAsync(string testerId)
        {
            try
            {
                var request = new GetTestHistoryRequest
                {
                    TesterId = testerId,
                    Page = 1,
                    PageSize = 1 // 只需要获取数量，所以设置为1
                };

                var response = await HttpService.PostAsync<ApiResponse<GetTestHistoryResponse>>("/test-records/history", request, GlobalMedicalStaffManager.CurrentToken);
                
                if (response.Success && response.Data != null)
                {
                    return response.Data.TotalCount;
                }
                
                return 0;
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                // 网络连接相关的异常，重新抛出让调用方处理
                throw;
            }
            catch (Exception ex)
            {
                // 其他异常，记录错误日志并返回 0
                System.Diagnostics.Debug.WriteLine($"获取测试历史数量失败: {ex.Message}");
                return 0;
            }
        }
    }
}
