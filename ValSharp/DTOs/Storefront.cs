using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValSharp.DTOs
{
    public class StorefrontOffer
    {
        public string OfferID { get; set; }
        public bool IsDirectPurchase { get; set; }
        public string StartDate { get; set; }
        public Dictionary<string, double> Cost { get; set; }
        public List<StorefrontOfferReward> Rewards { get; set; }
    }

    public class StorefrontOfferReward
    {
        public string ItemTypeID { get; set; }
        public string ItemID { get; set; }
        public int Quantity { get; set; }
    }

    public class Storefront
    {
        public StoreFeaturedBundle FeaturedBundle { get; set; }
        public BonusStore BonusStore { get; set; }
    }

    public class StoreFeaturedBundle
    {
        public StoreBundle Bundle { get; set; }
        public StoreBundle[] Bundles { get; set; }
        public int BundleRemainingDurationInSeconds { get; set; }
    }

    public class StoreBundle
    {
        public string ID { get; set; }
        public string DataAssetID { get; set; }
        public string CurrencyID { get; set; }
        public StoreItem[] Items { get; set; }
        public StoreItemOffer[] ItemOffers { get; set; }
        public Dictionary<string, int> TotalBaseCost { get; set; }
        public Dictionary<string, int> TotalDiscountedCost { get; set; }
        public int TotalDiscountPercent { get; set; }
        public int DurationRemainingInSeconds { get; set; }
        public bool WholesaleOnly { get; set; }
    }

    public class StoreItem
    {
        public StoreItemDetails Item { get; set; }
        public int BasePrice { get; set; }
        public string CurrencyID { get; set; }
        public int DiscountPercent { get; set; }
        public int DiscountedPrice { get; set; }
        public bool IsPromoItem { get; set; }
    }

    public class StoreItemDetails
    {
        public string ItemTypeID { get; set; }
        public string ItemID { get; set; }
        public int Amount { get; set; }
    }

    public class StoreItemOffer
    {
        public string BundleItemOfferID { get; set; }
        public StorefrontOffer Offer { get; set; }
        public int DiscountPercent { get; set; }
        public Dictionary<string, int> DiscountedCost { get; set; }
    }

    public class BonusStore
    {
        public BonusStoreOffer[] BonusStoreOffers { get; set; }
        public int BonusStoreRemainingDurationInSeconds { get; set; }
    }

    public class BonusStoreOffer
    {
        public string BonusOfferID { get; set; }
        public StorefrontOffer Offer { get; set; }
        public int DiscountPercent { get; set; }
        public Dictionary<string, int> DiscountCosts { get; set; }
        public bool IsSeen { get; set; }
    }
}
