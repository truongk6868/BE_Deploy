using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CondotelManagement.Models
{
    [Table("RefundRequests")]
    public partial class RefundRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(255)]
        public string CustomerName { get; set; } = null!;

        [StringLength(255)]
        public string? CustomerEmail { get; set; }
        
        // Thông tin hoàn tiền
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Refunded, Rejected
        
        // Thông tin ngân hàng
        [StringLength(50)]
        public string? BankCode { get; set; }

        [StringLength(50)]
        public string? AccountNumber { get; set; }

        [StringLength(255)]
        public string? AccountHolder { get; set; }
        
        // Thông tin xử lý
        [StringLength(500)]
        public string? Reason { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? CancelDate { get; set; }

        public int? ProcessedBy { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? ProcessedAt { get; set; }

        [StringLength(100)]
        public string? TransactionId { get; set; }

        [StringLength(50)]
        public string? PaymentMethod { get; set; } // 'Auto' (PayOS) hoặc 'Manual'
        
        // Resubmission tracking
        [Range(0, 1)]
        public int ResubmissionCount { get; set; } = 0; // Số lần user resubmit sau khi reject (max 1)
        
        // Timestamps
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; } = null!;

        [ForeignKey("CustomerId")]
        public virtual User Customer { get; set; } = null!;

        [ForeignKey("ProcessedBy")]
        public virtual User? ProcessedByUser { get; set; }
    }
}





