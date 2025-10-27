using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace BrainMirror.Services
{
    /// <summary>
    /// EDF文件写入器，用于实时保存脑电数据
    /// 基于正确的EDF格式规范
    /// </summary>
    public class EDFWriter : IDisposable
    {
        private FileStream fileStream;
        private BinaryWriter writer;
        private bool isDisposed = false;
        private bool isHeaderWritten = false;
        
        // EDF文件头信息
        private string patientId;
        private string recordingId;
        private DateTime startDate;
        private int numberOfDataRecords;
        private double dataRecordDuration;
        private int numberOfSignals;
        private int[] samplesPerDataRecord;
        private string[] signalLabels;
        private string[] transducerTypes;
        private string[] physicalDimensions;
        private double[] physicalMinimums;
        private double[] physicalMaximums;
        private int[] digitalMinimums;
        private int[] digitalMaximums;
        private string[] prefilterings;
        private int[] numberOfSamplesInDataRecord;
        
            // 数据缓冲区
        private List<double>[] signalBuffers;
        private int currentDataRecord = 0;
        
        public EDFWriter(string filePath, string patientId = "X X X X", string recordingId = "Startdate")
        {
            this.patientId = patientId;
            this.recordingId = recordingId;
            this.startDate = DateTime.Now;
            
            // 初始化EDF文件参数 - 匹配正确的EDF格式
            InitializeEDFParameters();
            
            // 创建文件流
            fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            writer = new BinaryWriter(fileStream, Encoding.ASCII);
            
            // 初始化信号缓冲区
            signalBuffers = new List<double>[numberOfSignals];
            for (int i = 0; i < numberOfSignals; i++)
            {
                signalBuffers[i] = new List<double>();
            }
            
        }
        
        private void InitializeEDFParameters()
        {
            // 设置EDF文件参数 - 匹配示例文件格式
            numberOfDataRecords = -1; // 动态记录数，稍后更新
            dataRecordDuration = 0.01; // 每个数据记录10ms，匹配dataRecordTimer的间隔
            numberOfSignals = 1; // 1个信号通道（脑电信号）
            
            // 信号参数 - 每个样本就是一个记录，不批量处理
            // 这样保存的数据点数就和CSV文件完全一致
            samplesPerDataRecord = new int[] { 1 }; // 每个记录只有1个样本
            signalLabels = new string[] { "FP1" }; // 匹配示例文件
            transducerTypes = new string[] { "EDF Annotations" }; // 匹配示例文件
            physicalDimensions = new string[] { "uV" };
            physicalMinimums = new double[] { -3000.0 }; // 匹配示例文件
            physicalMaximums = new double[] { 3000.0 }; // 匹配示例文件
            digitalMinimums = new int[] { -32767 }; // 匹配示例文件
            digitalMaximums = new int[] { 32767 }; // 匹配示例文件
            prefilterings = new string[] { "HP:0.5Hz LP:30Hz" };
            numberOfSamplesInDataRecord = new int[] { 1 };
        }
        
        /// <summary>
        /// 写入EDF文件头
        /// </summary>
        public void WriteHeader()
        {
            if (isHeaderWritten) return;
            
            // 写入固定长度头信息 - 按照EDF格式规范
            WriteFixedLengthString("0", 8); // 版本
            WriteFixedLengthString(patientId, 80); // 患者ID
            WriteFixedLengthString(recordingId, 80); // 记录ID
            WriteFixedLengthString(startDate.ToString("dd.MM.yy"), 8); // 开始日期
            WriteFixedLengthString(startDate.ToString("HH.mm.ss"), 8); // 开始时间
            WriteFixedLengthString("256", 8); // 头记录字节数
            WriteFixedLengthString("", 44); // 保留字段
            WriteFixedLengthString(numberOfDataRecords.ToString(), 8); // 数据记录数
            WriteFixedLengthString(dataRecordDuration.ToString("F6"), 8); // 数据记录持续时间
            WriteFixedLengthString(numberOfSignals.ToString(), 4); // 信号数
            
            // 写入信号参数
            foreach (var label in signalLabels)
                WriteFixedLengthString(label, 16);
            foreach (var transducer in transducerTypes)
                WriteFixedLengthString(transducer, 80);
            foreach (var dimension in physicalDimensions)
                WriteFixedLengthString(dimension, 8);
            foreach (var min in physicalMinimums)
                WriteFixedLengthString(min.ToString("F6"), 8);
            foreach (var max in physicalMaximums)
                WriteFixedLengthString(max.ToString("F6"), 8);
            foreach (var min in digitalMinimums)
                WriteFixedLengthString(min.ToString(), 8);
            foreach (var max in digitalMaximums)
                WriteFixedLengthString(max.ToString(), 8);
            foreach (var prefilter in prefilterings)
                WriteFixedLengthString(prefilter, 80);
            foreach (var samples in numberOfSamplesInDataRecord)
                WriteFixedLengthString(samples.ToString(), 8);
            
            // 写入保留字段（32字节）
            WriteFixedLengthString("", 32);
            
            isHeaderWritten = true;
        }
        
        /// <summary>
        /// 添加脑电数据样本
        /// </summary>
        /// <param name="sample">脑电数据样本</param>
        public void AddSample(double sample)
        {
            if (isDisposed) return;
            
            // 确保头已写入
            if (!isHeaderWritten)
            {
                WriteHeader();
            }
            
            // 直接将样本写入文件，不进行缓冲
            // 每个样本作为一个完整的EDF记录写入（因为samplesPerDataRecord[0] = 1）
            double physicalValue = Math.Max(physicalMinimums[0], Math.Min(physicalMaximums[0], sample));
            double normalizedValue = (physicalValue - physicalMinimums[0]) / (physicalMaximums[0] - physicalMinimums[0]);
            int digitalValue = (int)(digitalMinimums[0] + normalizedValue * (digitalMaximums[0] - digitalMinimums[0]));
            
            // 确保值在有效范围内
            digitalValue = Math.Max(digitalMinimums[0], Math.Min(digitalMaximums[0], digitalValue));
            
            // 写入16位整数（小端序）
            writer.Write((short)digitalValue);
            
            currentDataRecord++;
        }
        
        /// <summary>
        /// 写入一个完整的数据记录
        /// </summary>
        private void WriteDataRecord()
        {
            if (signalBuffers[0].Count < samplesPerDataRecord[0]) return;
            
            // 获取要写入的样本
            var samplesToWrite = signalBuffers[0].GetRange(0, samplesPerDataRecord[0]);
            
            // 转换为16位整数
            foreach (var sample in samplesToWrite)
            {
                // 将物理值转换为数字值
                double physicalValue = Math.Max(physicalMinimums[0], Math.Min(physicalMaximums[0], sample));
                double normalizedValue = (physicalValue - physicalMinimums[0]) / (physicalMaximums[0] - physicalMinimums[0]);
                int digitalValue = (int)(digitalMinimums[0] + normalizedValue * (digitalMaximums[0] - digitalMinimums[0]));
                
                // 确保值在有效范围内
                digitalValue = Math.Max(digitalMinimums[0], Math.Min(digitalMaximums[0], digitalValue));
                
                // 写入16位整数（小端序）
                writer.Write((short)digitalValue);
            }
            
            // 移除已写入的样本
            signalBuffers[0].RemoveRange(0, samplesPerDataRecord[0]);
            
            currentDataRecord++;
        }
        
        /// <summary>
        /// 写入固定长度的字符串
        /// </summary>
        private void WriteFixedLengthString(string value, int length)
        {
            if (value == null) value = "";
            if (value.Length > length) value = value.Substring(0, length);
            
            byte[] bytes = Encoding.ASCII.GetBytes(value.PadRight(length));
            writer.Write(bytes);
        }
        
        /// <summary>
        /// 完成写入并关闭文件
        /// </summary>
        public void Finish()
        {
            if (isDisposed) return;
            
            // 更新头中的记录数
            if (currentDataRecord > 0)
            {
                fileStream.Seek(236, SeekOrigin.Begin); // 记录数位置
                WriteFixedLengthString(currentDataRecord.ToString(), 8);
            }
            
            Dispose();
        }
        
        public void Dispose()
        {
            if (!isDisposed)
            {
                writer?.Dispose();
                fileStream?.Dispose();
                isDisposed = true;
            }
        }
    }
}