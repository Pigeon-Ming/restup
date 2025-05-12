using Restup.HttpMessage.Models.Contracts;
using Restup.HttpMessage.Plumbing;
using Restup.WebServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Restup.HttpMessage.ServerRequestParsers
{
    internal class HttpRequestParser
    {
        private const uint BUFFER_SIZE = 8192;

        internal static HttpRequestParser Default { get; }

        static HttpRequestParser()
        {
            Default = new HttpRequestParser();
        }

        private IEnumerable<IHttpRequestPartParser> GetPipeline()
        {
            return new IHttpRequestPartParser[]
            {
                    new MethodParser(),
                    new ResourceIdentifierParser(),
                    new ProtocolVersionParser(),
                    new HeadersParser(),
                    new ContentParser()
            };
        }

        internal async Task<MutableHttpServerRequest> ParseRequestStream(IInputStream requestStream)
        {
            // 创建一个 HttpRequestStream 对象，用于读取 HTTP 请求流
            var httpStream = new HttpRequestStream(requestStream);
            // 初始化一个可变的 HTTP 服务器请求对象
            var request = new MutableHttpServerRequest();

            try
            {
                // 从 HTTP 流中读取数据，指定缓冲区大小和读取选项
                var stream = await httpStream.ReadAsync(BUFFER_SIZE, InputStreamOptions.Partial);
                // 获取读取到的数据字节数组
                byte[] streamData = stream.Data;

                // 获取解析请求的管道（多个解析器）
                var requestPipeline = GetPipeline();
                // 创建管道的枚举器以逐步处理解析器
                using (var pipeLineEnumerator = requestPipeline.GetEnumerator())
                {
                    // 移动到第一个解析器
                    pipeLineEnumerator.MoveNext();
                    // 标记请求是否已完成
                    bool requestComplete = false;

                    // 循环处理请求，直到完成
                    while (!requestComplete)
                    {
                        // 当前解析器处理请求的一部分
                        pipeLineEnumerator.Current.HandleRequestPart(streamData, request);
                        // 获取未解析的数据
                        streamData = pipeLineEnumerator.Current.UnparsedData;

                        // 如果当前解析器完成了
                        if (pipeLineEnumerator.Current.IsFinished)
                        {
                            // 检查解析是否成功，或者是否还有下一个解析器
                            if (!pipeLineEnumerator.Current.IsSucceeded ||
                                !pipeLineEnumerator.MoveNext())
                            {
                                // 如果失败或没有下一个解析器，则退出循环
                                break;
                            }
                        }
                        else
                        {
                            // 从 HTTP 流中读取更多数据
                            var newStreamdata = await httpStream.ReadAsync(BUFFER_SIZE, InputStreamOptions.Partial);

                            // 如果读取失败，则退出循环
                            if (!newStreamdata.ReadSuccessful)
                            {
                                break;
                            }

                            // 将新读取的数据与未解析的数据合并
                            streamData = streamData.ConcatArray(newStreamdata.Data);
                        }
                    }
                }

                // 检查管道中所有解析器是否都成功完成
                request.IsComplete = requestPipeline.All(p => p.IsSucceeded);
                Debug.WriteLine($"URI:{request.Uri.ToString()}");

                OperationController.TryRunOperationByRequestUri(request);//根据请求的URI执行功能

                if (request.Content != null)
                {
                    Debug.WriteLine($"RequestContentLength: {request.Content.Length}");
                    RestupTest.requestContent = request.Content;
                    Debug.WriteLine("byte[]数据已保存至requestContent");
                }
                
            }
            catch (Exception ex)
            {
                // 捕获异常并输出调试信息
                Debug.WriteLine(ex.Message);
            }

            // 返回解析后的请求对象
            return request;
        }
    }
}
