using System.Collections.Generic;

namespace Paylink
{
    /// <summary>Every webhook event type PayLink can deliver.</summary>
    public static class WebhookEventType
    {
        public const string InvoicePaid = "invoice.paid";
        public const string InvoiceNotPaid = "invoice.not_paid";
        public const string InvoiceVoided = "invoice.voided";
        public const string InvoiceRefunded = "invoice.refunded";
        public const string InvoiceAuthorizationReversed = "invoice.authorization_reversed";
        public const string InvoiceAuthorized = "invoice.authorized";
        public const string SubscriptionActivated = "subscription.activated";
        public const string SubscriptionCharged = "subscription.charged";
        public const string SubscriptionPaymentFailed = "subscription.payment_failed";
        public const string SubscriptionSuspended = "subscription.suspended";
        public const string SubscriptionCancelled = "subscription.cancelled";
        public const string SubscriptionCompleted = "subscription.completed";
        public const string InstallmentPlanActivated = "installment.plan_activated";
        public const string InstallmentCharged = "installment.charged";
        public const string InstallmentPaymentFailed = "installment.payment_failed";
        public const string InstallmentPlanSuspended = "installment.plan_suspended";
        public const string InstallmentPlanCompleted = "installment.plan_completed";
        public const string VccPaymentSuccess = "vcc.payment_success";
        public const string VccPaymentFailed = "vcc.payment_failed";
        public const string CardTokenChargeSucceeded = "card_token.charge_succeeded";
        public const string CardTokenChargeFailed = "card_token.charge_failed";
    }

    /// <summary>A verified webhook event; <see cref="Raw"/> holds the full decoded body.</summary>
    public sealed class WebhookEvent
    {
        public WebhookEvent(
            string @event,
            bool success,
            IReadOnlyDictionary<string, object?> raw,
            long? invoiceId,
            string? invoiceStatus,
            string? message,
            string? authCode,
            string? mandateId,
            string? externalReference,
            string? subscriptionStatus,
            string? eventTriggeredAt,
            string? timezone)
        {
            Event = @event;
            Success = success;
            Raw = raw;
            InvoiceId = invoiceId;
            InvoiceStatus = invoiceStatus;
            Message = message;
            AuthCode = authCode;
            MandateId = mandateId;
            ExternalReference = externalReference;
            SubscriptionStatus = subscriptionStatus;
            EventTriggeredAt = eventTriggeredAt;
            Timezone = timezone;
        }

        public string Event { get; }

        public bool Success { get; }

        public IReadOnlyDictionary<string, object?> Raw { get; }

        public long? InvoiceId { get; }

        public string? InvoiceStatus { get; }

        public string? Message { get; }

        public string? AuthCode { get; }

        public string? MandateId { get; }

        public string? ExternalReference { get; }

        public string? SubscriptionStatus { get; }

        public string? EventTriggeredAt { get; }

        public string? Timezone { get; }
    }
}
