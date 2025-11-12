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
            // 设置EDF文件参数 - 匹配示例文件格式（EDF+C格式）
            numberOfDataRecords = -1; // 动态记录数，稍后更新
            dataRecordDuration = 1.0; // 每个数据记录1秒
            numberOfSignals = 2; // 2个信号通道（脑电信号 + EDF Annotations）
            
            // 信号参数 - 520Hz采样率，每秒520个样本
            samplesPerDataRecord = new int[] { 520, 60 }; // FP1: 520样本/秒, Annotations: 60样本/秒
            signalLabels = new string[] { "FP1", "EDF Annotations" };
            transducerTypes = new string[] { "", "" }; // 传感器类型为空
            physicalDimensions = new string[] { "uV", "" }; // FP1用uV，Annotations为空
            physicalMinimums = new double[] { -900.0, -1.0 }; // 匹配原软件
            physicalMaximums = new double[] { 899.0, 1.0 }; // 匹配原软件
            digitalMinimums = new int[] { -4096, -32768 }; // 匹配原软件
            digitalMaximums = new int[] { 4095, 32767 }; // 匹配原软件
            prefilterings = new string[] { "", "" }; // 预滤波为空
            numberOfSamplesInDataRecord = new int[] { 520, 60 };
        }
        
        /// <summary>
        /// 写入EDF文件头
        /// </summary>
        public void WriteHeader()
        {
            if (isHeaderWritten) return;
            
            // 计算头部字节数：256（固定头）+ 256 * 信号数量
            int headerBytes = 256 + 256 * numberOfSignals;
            
            // 写入固定长度头信息 - 按照EDF格式规范
            WriteFixedLengthString("0", 8); // 版本
            WriteFixedLengthString(patientId, 80); // 患者ID
            WriteFixedLengthString(recordingId, 80); // 记录ID
            WriteFixedLengthString(startDate.ToString("dd.MM.yy"), 8); // 开始日期
            WriteFixedLengthString(startDate.ToString("HH.mm.ss"), 8); // 开始时间
            WriteFixedLengthString(headerBytes.ToString(), 8); // 头记录字节数（正确计算）
            WriteFixedLengthString("EDF+C", 44); // 保留字段 - 标记为EDF+C格式（连续记录）
            WriteFixedLengthString(numberOfDataRecords.ToString(), 8); // 数据记录数
            WriteFixedLengthString(dataRecordDuration.ToString("F0"), 8); // 数据记录持续时间（1秒）
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
            
            // 将样本添加到缓冲区
            signalBuffers[0].Add(sample);
            
            // 当缓冲区达到一个数据记录的样本数时，写入数据记录
            if (signalBuffers[0].Count >= samplesPerDataRecord[0])
            {
                WriteDataRecord();
            }
        }
        
        /// <summary>
        /// 写入一个完整的数据记录
        /// </summary>
        private void WriteDataRecord()
        {
            if (signalBuffers[0].Count < samplesPerDataRecord[0]) return;
            
            // 写入第一个信号（FP1脑电数据）的样本
            var samplesToWrite = signalBuffers[0].GetRange(0, samplesPerDataRecord[0]);
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
            
            // 写入第二个信号（EDF Annotations）
            // EDF Annotations格式：时间标记 + 持续时间（可选）+ 标注（可选）
            // 对于连续记录，我们写入时间标记
            WriteEDFAnnotations(currentDataRecord * dataRecordDuration);
            
            currentDataRecord++;
        }
        
        /// <summary>
        /// 写入EDF Annotations数据
        /// </summary>
        /// <param name="timeOffset">时间偏移（秒）</param>
        private void WriteEDFAnnotations(double timeOffset)
        {
            // EDF Annotations使用TAL（Time-stamped Annotations List）格式
            // 格式："+时间\x14持续时间\x14标注\x14\x00"
            // 对于记录开始标记："+0\x14\x14\x00" 或 "+时间\x14Recording starts\x14\x00"
            
            string annotation;
            if (currentDataRecord == 0)
            {
                // 第一个记录：标记记录开始
                annotation = $"+{timeOffset:F1}\x14\x14Recording starts\x14\x00";
            }
            else
            {
                // 其他记录：只写时间戳
                annotation = $"+{timeOffset:F1}\x14\x14\x14\x00";
            }
            
            // 将标注转换为字节并填充到60个样本（120字节，因为每个样本是2字节）
            byte[] annotationBytes = Encoding.UTF8.GetBytes(annotation);
            
            // 写入标注数据，每个样本2字节，共60个样本 = 120字节
            int bytesWritten = 0;
            for (int i = 0; i < annotationBytes.Length && bytesWritten < 120; i++)
            {
                writer.Write((short)annotationBytes[i]);
                bytesWritten += 2;
            }
            
            // 填充剩余字节为0
            while (bytesWritten < 120)
            {
                writer.Write((short)0);
                bytesWritten += 2;
            }
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
            
            // 写入剩余的样本（如果有）
            if (signalBuffers[0].Count > 0)
            {
                // 填充到完整的数据记录
                while (signalBuffers[0].Count < samplesPerDataRecord[0])
                {
                    signalBuffers[0].Add(0); // 用0填充
                }
                WriteDataRecord();
            }
            
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