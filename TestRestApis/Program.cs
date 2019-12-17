using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRestApis
{
    class Program
    {


        static void Main(string[] args)
        {

            string uuid = "ab9122a1-8a7e-585b-9857-4cb5de7d4841";
            string orderId = string.Empty;

            VatValues vatValues = new VatValues();

            //Do the checkbasket
            IRestResponse checkResult = PostCheckBasket(uuid);
            Console.WriteLine("1) CheckBasket Contents = \n" + checkResult.Content);

            //deserialise the contents of checkbasket
            dynamic checkBasket = JsonConvert.DeserializeObject<dynamic>(checkResult.Content);


            //check if checkbasket succeeded
            if (checkBasket.type == "CALCULATE_REQUEST_SUCCEEDED")
            {
                Console.WriteLine("CheckBasket succeeded - do the order next and fill the vat values");
                Console.WriteLine($"Tax:{checkBasket.data.final.tax}");
                Console.WriteLine($"Excluding Tax:{checkBasket.data.final.excTax}");
                Console.WriteLine($"Including Tax:{checkBasket.data.final.incTax}");
                Console.WriteLine($"Total:{checkBasket.data.final.total}");

                Console.WriteLine($"Time stamp: {checkBasket.timestamp}");
                Console.WriteLine($" Type: {checkBasket.type}");
                vatValues.Tax = checkBasket.data.final.tax;
                vatValues.ExTax = checkBasket.data.final.excTax;
                vatValues.IncTax = checkBasket.data.final.incTax;
                vatValues.Total = checkBasket.data.final.total;

                IRestResponse orderResult = PostOrder(uuid);
                Console.WriteLine("\n2) Order Contents = \n" + orderResult.Content);

                //deserialise the contents of order return class details
                dynamic order = JsonConvert.DeserializeObject<dynamic>(orderResult.Content);

                Console.WriteLine($"Order Id:{order.orderId}");
                orderId = order.orderId;
                if (string.IsNullOrEmpty(orderId)) Console.WriteLine("Order Failed");
                else
                {
                    Console.WriteLine("Order succeeded - do the Fullfillment");
                    IRestResponse fullfillmentResult = Fullfillment(uuid, orderId);

                    Console.WriteLine("\n3) Fullfillment Contents = \n" + fullfillmentResult.Content);

                    //deserialise the contents fullfillment return class details
                    dynamic fulfillment = JsonConvert.DeserializeObject<dynamic>(fullfillmentResult.Content);

                    if (fullfillmentResult.IsSuccessful)
                    {
                        Console.WriteLine("Fullfillment stage passed. Do Mark as paid");

                        IRestResponse markAsPaidResult = MarkAsPaid(vatValues, orderId);
                        Console.WriteLine("\n4) Mark as Paid Contents = \n" + markAsPaidResult.Content);

                        //deserialise the contentsof markAsPaid return class details
                        dynamic markAsPaid = JsonConvert.DeserializeObject<dynamic>(markAsPaidResult.Content);

                        if (markAsPaidResult.IsSuccessful)
                        {
                            Console.WriteLine("Mark as Paid stage passed.  send to POS");

                            IRestResponse sendToPOSResult = SendToPOS(orderId);
                            Console.WriteLine("\n5) Send to POS Contents = \n" + sendToPOSResult.Content);

                            //Expose the orderResult return class details
                            dynamic sendToPOS = JsonConvert.DeserializeObject<dynamic>(sendToPOSResult.Content);
                            Console.WriteLine($"OrderId = {sendToPOS.data.orderId}");
                            Console.WriteLine($"output = {sendToPOS.type}");
                        }
                        else
                            Console.WriteLine("Mark as Paid stage Failed.");
                    }
                    else
                        Console.WriteLine("Fullfillment stage Failed.");
                }
            }
            else
            {
                Console.WriteLine("CheckBasket Failed");
            }

            Console.WriteLine("\n Transaction finished - Press any key to exit...");
            Console.ReadKey();
        }

        public static IRestResponse PostCheckBasket(string uuid)
        {
            Random rand = new Random();
            int randNum = rand.Next(1, 10000);
            Console.WriteLine($"version number = {randNum}");

            var client = new RestClient("https://flyt-acrelec-integration.flyt-platform.com/checkBasket");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Connection", "keep-alive");
            request.AddHeader("Host", "flyt-acrelec-integration.flyt-platform.com");
            request.AddHeader("Accept", "*/*");
            request.AddHeader("Content-Type", "text/plain");
            request.AddHeader("X-Flyt-API-Key", "hdgskIZRgBmyArKCtzkjkZIvaBjMkXVbWGvbq");
            request.AddParameter("undefined", "{\r\n  \"locationId\": 5156,\r\n  \"items\": [\r\n    {\r\n      \"uuid\": \"" + uuid + "\",\r\n      \"modifiers\": []\r\n    }\r\n  ],\r\n  \"externalReference\": \"" + randNum + "\",\r\n  \"fulfillmentType\": \"collection-by-customer\",\r\n  \"version\": " + randNum + ",\r\n  \"notifications\": [\r\n    {\r\n      \"type\": \"webhook\",\r\n      \"uri\": \"https://flyt-acrelec-integration.flyt-platform.com/receiveTaxWebhook?X-Flyt-Api-Key=hdgskIZRgBmyArKCtzkjkZIvaBjMkXVbWGvbq\"\r\n    }\r\n  ]\r\n}", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            return response;
        }

        public static IRestResponse PostOrder(string uuid)
        {
            var client = new RestClient("https://api.flypaythis.com/ordering/v3/order");
            var request = new RestRequest(Method.POST);     
            request.AddHeader("Connection", "keep-alive");
            request.AddHeader("Host", "api.flypaythis.com");
            request.AddHeader("Accept", "*/*");
            request.AddHeader("Content-Type", "text/plain");
            request.AddHeader("X-Flypay-API-Key", "u7f2r48x6bzwyy09vwsii");
            request.AddParameter("undefined", "{\n  \"locationId\": 5156,\n  \"items\": [\n    {\n      \"uuid\": \"" + uuid + "\",\n      \"modifiers\": [\n      ]\n    }\n  ]\n}", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            return response;
        }

        public static IRestResponse Fullfillment(string uuid, string orderId)
        {
            Random randNum = new Random();
            int orderRef = randNum.Next(1, 1000);
            Console.WriteLine($"Order Reference = {orderRef}");

            var client = new RestClient("https://api.flypaythis.com/ordering/v3/order/" + orderId + "/fulfillment-type/collection-by-customer");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Connection", "keep-alive");
            request.AddHeader("Host", "api.flypaythis.com");
            request.AddHeader("Accept", "*/*");
            request.AddHeader("Content-Type", "text/plain");
            request.AddHeader("X-Flypay-API-Key", "u7f2r48x6bzwyy09vwsii");
            request.AddParameter("undefined", "{  \r\n  \"collectionTime\": \"2019-12-21T15:16:36+00:00\",  \r\n  \"collectionNotes\": \"10\",  \r\n  \"kitchenNotes\": \"No nuts please\",  \r\n  \"name\": \"Kiosk 1\",  \r\n  \"phone\": \"+44 1234 5678 90\",  \r\n  \"orderReference\": \"" + orderRef + "\"\r\n} ", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            return response;
        }
       
        public static IRestResponse MarkAsPaid(VatValues vatvalues, string orderId)
        {

            Random randNum = new Random();
            int extRef = randNum.Next(1, 100);

            Console.WriteLine($"External Reference = {extRef}");

            var client = new RestClient("https://api.flypaythis.com/ordering/v3/order/" + orderId + "/mark-as-paid");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Connection", "keep-alive");
            request.AddHeader("Host", "api.flypaythis.com");
            request.AddHeader("Accept", "*/*");
            request.AddHeader("Content-Type", "text/plain");
            request.AddHeader("X-Flypay-API-Key", "u7f2r48x6bzwyy09vwsii");
            request.AddParameter("undefined", "{\r\n\t\"paymentMethod\": \"card\",\r\n\t\"payment\": {\r\n\t\t\"itemsInCart\": {\r\n\t\t\t\"incTax\":"+ vatvalues.IncTax + ",\r\n\t\t\t\"tax\": "+ vatvalues.Tax + "\r\n\t\t},\r\n\t\t\"adjustments\": [{\r\n\t\t\t\t\"externalReference\": \"" + extRef + "\",\r\n\t\t\t\t\"name\": \"collection-by-customer\",\r\n\t\t\t\t\"price\": {\r\n\t\t\t\t\t\"incTax\":" + vatvalues.IncTax + ",\r\n\t\t\t\t\t\"tax\":" + vatvalues.Tax + "\r\n\t\t\t\t}\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"name\": \"Bag Fee\",\r\n\t\t\t\t\"price\": {\r\n\t\t\t\t\t\"incTax\":" + vatvalues.IncTax + ",\r\n\t\t\t\t\t\"tax\":" + vatvalues.Tax + "\r\n\t\t\t\t}\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"final\": {\r\n\t\t\t\"excTax\":" + vatvalues.ExTax + ",\r\n\t\t\t\"incTax\":" + vatvalues.IncTax + ",\r\n\t\t\t\"tax\":"+ vatvalues.Tax + ",\r\n\t\t\t\"total\":" + vatvalues.Total + "\r\n\t\t}\r\n\t}\r\n}", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            return response;
        }

        public static IRestResponse SendToPOS(string orderId)
        {

            var client = new RestClient("https://flyt-acrelec-integration.flyt-platform.com/sendToPos");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Connection", "keep-alive");
            request.AddHeader("Accept-Encoding", "gzip, deflate");
            request.AddHeader("Host", "flyt-acrelec-integration.flyt-platform.com");
            request.AddHeader("Accept", "*/*");
            request.AddHeader("Content-Type", "text/plain");
            request.AddHeader("X-Flyt-API-Key", "hdgskIZRgBmyArKCtzkjkZIvaBjMkXVbWGvbq");
            request.AddParameter("undefined", "{\r\n    \"orderId\": \""+ orderId + "\"\r\n}", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            return response;
        }
    }

    public class VatValues
    {
        public int ExTax { get; set; }
        public int IncTax { get; set; }
        public int Tax { get; set; }
        public int Total { get; set; }
    }
}
