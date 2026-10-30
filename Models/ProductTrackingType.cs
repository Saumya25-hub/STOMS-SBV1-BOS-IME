namespace STOMS_SBV1_BOS_IME.Models
{
    /// <summary>
    /// Specifies the tracking granularity configured for a product.
    /// Controls dynamic field visibility and validation during stock inward/outward operations.
    /// </summary>
    public enum ProductTrackingType
    {
        /// <summary>
        /// Standard bulk/quantity tracking without batch or individual item serialization.
        /// </summary>
        QuantityOnly = 0,

        /// <summary>
        /// Lot/Batch tracking with unique batch numbers, manufacturing dates, and FEFO expiry tracking.
        /// </summary>
        BatchTracked = 1,

        /// <summary>
        /// Individual serialization tracking where every piece has a unique serial number.
        /// </summary>
        SerialTracked = 2,

        /// <summary>
        /// High-precision dual tracking: items belong to a production batch and each piece carries a unique serial number.
        /// </summary>
        BatchAndSerial = 3
    }
}

