using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using DealParser.AppConstants;

namespace DealParser.Services
{
    public static class ClientHub
    {
        /// <summary>
        /// Возвращает общее количество сделок посредством запроса в формате GraphQL
        /// </summary>
        /// <returns> Количество записей о сделках </returns>
        public static int GetCountOfDeals()
        {
            var query =
                "{\"query\":\"query SearchReportWoodDealCount(\\r\\n  $size: Int!\\r\\n  $number: Int!\\r\\n  $filter: Filter\\r\\n  $orders: [Order!]\\r\\n) {\\r\\n  searchReportWoodDeal(\\r\\n    filter: $filter\\r\\n    pageable: { number: $number, size: $size }\\r\\n    orders: $orders\\r\\n  ) {\\r\\n    total\\r\\n}\\r\\n}\\r\\n\",\"variables\":{\"size\":20,\"number\":2,\"filter\":null}}";

            var strResp = SendGraphQL_Request(query);
            var json = JsonNode.Parse(strResp);
                
            return json["data"]["searchReportWoodDeal"]["total"].GetValue<int>();
        }
        
        /// <summary>
        /// Запрашивает следующий чанк записей
        /// </summary>
        /// <param name="pageSize"> Размер страницы-чанка </param>
        /// <param name="pageNumber"> Номер текущей страницы </param>
        /// <returns> JSON - строка с массивом внутри тега "content" </returns>
        public static string GetNextPage(int pageSize = 20, int pageNumber = 0)
        {
            var query = "{\"query\":\"query SearchReportWoodDeal($size: Int!, $number: Int!, $filter: Filter, $orders: [Order!]) \\r\\n{\\r\\n    searchReportWoodDeal(filter: $filter, pageable: {number: $number, size: $size}, orders: $orders) \\r\\n    {\\r\\n        content\\r\\n        {\\r\\n           sellerName\\r\\n           sellerInn\\r\\n           buyerName\\r\\n           buyerInn\\r\\n           woodVolumeBuyer\\r\\n           woodVolumeSeller\\r\\n           dealDate\\r\\n           dealNumber\\r\\n        }\\r\\n    }\\r\\n}\"," + 
                        "\"variables\":{\"" +
                        $"size\":{pageSize},\"" +
                        $"number\":{pageNumber},\"" +
                        "filter\":null,\"" +
                        "orders\":null}}}}";

            return SendGraphQL_Request(query);
        }

        /// <summary>
        /// Отправляет GraphQL запрос через HTTPWebRequest, возвращает JSON строку с результатом
        /// </summary>
        /// <param name="query"> Запрос GraphQL </param>
        /// <returns> JSON-строка с результатомы </returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static string SendGraphQL_Request(string query)
        {
            var byteArray = Encoding.UTF8.GetBytes(query);
            
            var request = (HttpWebRequest) WebRequest.Create(AppConstant.HOST_GRAPHQL);
            request.ContentType = "application/json";
            request.Method = "POST";
            request.Accept = "*/*";
            request.UserAgent = AppConstant.USER_AGENT;
            request.ConnectionGroupName = "keep-alive";
            request.ContentLength = byteArray.Length;

            // Записываем данные в тело запроса
            using (var dataStream =  request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            var response = request.GetResponse();
        
            // Получаем ответ через поток и возвращаем уже строкуы
            using (var stream = new StreamReader(
                       response.GetResponseStream() ?? throw new InvalidOperationException(), Encoding.UTF8))
            {
                return stream.ReadToEnd();
            }
        }
    }
}