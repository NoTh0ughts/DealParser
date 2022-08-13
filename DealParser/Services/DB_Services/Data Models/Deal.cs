using System;
using System.Text.Json.Nodes;

namespace DealParser.Services.DB_Services.Data_Models
{
    /// <summary>
    /// Data- класс содержит информацию
    /// Содержит также информацию продавца и покупателя в связи один ко многим
    /// Объем товара храниться в типе decimal, что позволяет избежать ошибки округления
    /// </summary>
    public class Deal
    {
        public decimal WoodVolumeBuyer { get; set; }
        public decimal WoodVolumeSeller { get; set; }
        public DateTime Date { get; set; }
        public string Number { get; set; }
        
        public Seller Seller { get; set; }
        public Buyer Buyer { get; set; }

        public Deal()
        {
        }
        public Deal(JsonNode json)
        {
            Number = json["dealNumber"]?.GetValue<string>();
            WoodVolumeBuyer = json["woodVolumeBuyer"]?.GetValue<decimal>() ?? 0;
            WoodVolumeSeller = json["woodVolumeSeller"]?.GetValue<decimal>() ?? 0;
            
            Date = DateTime.Parse(json["dealDate"]?.GetValue<string>());
            
            Seller = new Seller
            {
                Inn = json["sellerInn"]?.GetValue<string>(),
                Name = json["sellerName"]?.GetValue<string>()
            };

            Buyer = new Buyer
            {
                Inn = json["buyerInn"]?.GetValue<string>(),
                Name = json["buyerName"]?.GetValue<string>()
            };
        }
        

        public bool Equals(Deal other)
        {
            if (other is null) return false;
            return WoodVolumeBuyer == other.WoodVolumeBuyer &&
                   WoodVolumeSeller == other.WoodVolumeSeller &&
                   Date.Equals(other.Date) &&
                   Number == other.Number &&
                   Seller.Inn == other.Seller?.Inn &&
                   Seller.Name == other.Seller?.Name &&
                   Buyer.Inn == other.Buyer?.Inn &&
                   Buyer.Name == other.Buyer?.Name;
        }
    }
}