using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHangOnline.Models.EF
{
    [Table("tb_Review")]
    public class ReviewProduct
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("Product")]
        [Required(ErrorMessage = "Mã sản phẩm là bắt buộc.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Tên người dùng là bắt buộc.")]
        [MaxLength(100, ErrorMessage = "Tên người dùng không được vượt quá 100 ký tự.")]
        public string UserName { get; set; }

        [MaxLength(200, ErrorMessage = "Họ và tên không được vượt quá 200 ký tự.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        [MaxLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Nội dung đánh giá là bắt buộc.")]
        [MaxLength(1000, ErrorMessage = "Nội dung đánh giá không được vượt quá 1000 ký tự.")]
        public string Content { get; set; }

        [Required(ErrorMessage = "Đánh giá là bắt buộc.")]
        [Range(1, 5, ErrorMessage = "Đánh giá phải nằm trong khoảng từ 1 đến 5.")]
        public int Rate { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [MaxLength(255)]
        public string Avatar { get; set; }

        public virtual Product Product { get; set; }
    }
}
