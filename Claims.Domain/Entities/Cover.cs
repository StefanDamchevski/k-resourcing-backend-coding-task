using Claims.Domain.Enums;
using MongoDB.Bson.Serialization.Attributes;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Claims.Domain.Entities
{
    public class Cover
    {
        [BsonId]
        public string Id { get; set; }

        [BsonElement("startDate")]
        public DateOnly StartDate { get; set; }

        [BsonElement("endDate")]
        public DateOnly EndDate { get; set; }

        [BsonElement("claimType")]
        public CoverType Type { get; set; }

        [BsonElement("premium")]
        public decimal Premium { get; set; }
    }
}