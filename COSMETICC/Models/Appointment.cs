using System;
using System.ComponentModel.DataAnnotations;

namespace COSMETICC.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn dịch vụ")]
        [StringLength(150)]
        public string ServiceType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn chi nhánh")]
        [StringLength(150)]
        public string BranchLocation { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DoctorName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày hẹn")]
        public DateTime AppointmentDate { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Vui lòng chọn khung giờ")]
        [StringLength(50)]
        public string TimeSlot { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Chờ xác nhận";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? UserId { get; set; }
    }
}
