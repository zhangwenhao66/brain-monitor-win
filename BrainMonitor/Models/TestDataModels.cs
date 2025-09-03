using System;
using System.Collections.Generic; // Added for List

namespace BrainMirror.Models
{
    // 上传响应数据类
    public class UploadResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public UploadData? data { get; set; }
    }
    
    public class UploadData
    {
        public int testResultId { get; set; }
        public int brainwaveDataId { get; set; }
        public string? dataType { get; set; }
        public string? csvFilePath { get; set; }
        public string? fileName { get; set; }
    }
    
    // 测试结果事件参数类
    public class TestResultsEventArgs : EventArgs
    {
        public int OpenEyesResultId { get; set; }
        public int ClosedEyesResultId { get; set; }
        
        // 脑电处理结果
        public double ThetaValue { get; set; }
        public double AlphaValue { get; set; }
        public double BetaValue { get; set; }
        public double BrainwaveFinalIndex { get; set; }
        
        // 闭眼测试数据
        public List<double> ClosedEyesData { get; set; } = new List<double>();
    }
}
