using Restup.HttpMessage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalSend.Models;

namespace Restup.WebServer
{
    public class OperationController
    {
        public static Dictionary<string, Func<MutableHttpServerRequest, object>> UriOperations { get; set; } = new Dictionary<string, Func<MutableHttpServerRequest, object>>();

        public static void TryRunOperationByRequestUri(MutableHttpServerRequest mutableHttpServerRequest)
        {
            //Debug.WriteLine($"uri:{mutableHttpServerRequest.Uri.ToString()}");
            string uri = StringHelper.GetURLFromURLWithQueryParmeters(mutableHttpServerRequest.Uri.ToString());
            Debug.WriteLine($"正在寻找uri:{uri}的托管函数");
            if(UriOperations.ContainsKey(uri))
            {
                Debug.WriteLine($"准备执行uri:{uri}的托管函数");
                Func<MutableHttpServerRequest,object> func = UriOperations[uri];
                func(mutableHttpServerRequest);
            }
        }
    }
}
