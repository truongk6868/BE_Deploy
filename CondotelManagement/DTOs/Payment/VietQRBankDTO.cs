namespace CondotelManagement.DTOs.Payment
{
    public class VietQRBankListResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public List<VietQRBankDTO> Data { get; set; } = new();
    }

    public class VietQRBankDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Bin { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string Logo { get; set; } = string.Empty;
        public int TransferSupported { get; set; }
        public int LookupSupported { get; set; }
    }
}
