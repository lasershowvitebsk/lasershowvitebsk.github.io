using System;
using System.ComponentModel.DataAnnotations;

namespace LaserShowSite.Models
{
    public class RequestModel
    {
        // ──────────────── Имя ────────────────

        [Required(ErrorMessageResourceType = typeof(Resources.Models.RequestModel),
                  ErrorMessageResourceName = "Name_Required")]
        [StringLength(50, MinimumLength = 2,
                      ErrorMessageResourceType = typeof(Resources.Models.RequestModel),
                      ErrorMessageResourceName = "Name_StringLength")]
        // Первая буква обязательна, далее буквы, дефисы или одиночные пробелы
        [RegularExpression(@"^[a-zA-Zа-яА-ЯёЁ]+([\s\-][a-zA-Zа-яА-ЯёЁ]+)*$",
                           ErrorMessageResourceType = typeof(Resources.Models.RequestModel),
                           ErrorMessageResourceName = "Name_InvalidCharacters")]
        public string? Name { get; set; }

        // ──────────────── Телефон ────────────────

        [Required(ErrorMessageResourceType = typeof(Resources.Models.RequestModel),
                  ErrorMessageResourceName = "Phone_Required")]
        [RegularExpression(@"^\+375-(25|29|33|44)-[0-9]{3}-[0-9]{2}-[0-9]{2}$",
                           ErrorMessageResourceType = typeof(Resources.Models.RequestModel),
                           ErrorMessageResourceName = "Phone_InvalidFormat")]
        public string? Phone { get; set; }

        // ──────────────── Email ────────────────
        // [EmailAddress] уже проверяет формат — дублирующий [RegularExpression] убран,
        // чтобы при ошибке не показывались два одинаковых сообщения.

        [StringLength(100, ErrorMessageResourceType = typeof(Resources.Models.RequestModel),
                      ErrorMessageResourceName = "Email_StringLength")]
        [EmailAddress(ErrorMessageResourceType = typeof(Resources.Models.RequestModel),
                      ErrorMessageResourceName = "Email_EmailAddress")]
        public string? Email { get; set; }

        // ──────────────── Тип мероприятия ────────────────

        [Required(ErrorMessageResourceType = typeof(Resources.Models.RequestModel),
                  ErrorMessageResourceName = "EventType_Required")]
        public string? EventType { get; set; }

        // ──────────────── Дата мероприятия ────────────────

        [Required(ErrorMessageResourceType = typeof(Resources.Models.RequestModel),
                  ErrorMessageResourceName = "EventDate_Required")]
        public DateTime? EventDate { get; set; }

        // ──────────────── Количество гостей ────────────────

        public string? Guests { get; set; }

        // ──────────────── Детали ────────────────

        [Required(ErrorMessageResourceType = typeof(Resources.Models.RequestModel),
                  ErrorMessageResourceName = "Details_Required")]
        [StringLength(1000, MinimumLength = 6,
                      ErrorMessageResourceType = typeof(Resources.Models.RequestModel),
                      ErrorMessageResourceName = "Details_StringLength")]
        public string? Details { get; set; }
    }
}