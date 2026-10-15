using Claims.Domain.Enums;
using MongoDB.Bson.Serialization.Attributes;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Claims.Domain.Entities
{
    public class Claim
    {
        [BsonId]
        public string Id { get; set; }

        [BsonElement("coverId")]
        public string CoverId { get; set; }

        [BsonElement("created")]
        public DateOnly Created { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("claimType")]
        public ClaimType Type { get; set; }

        [BsonElement("damageCost")]
        public decimal DamageCost { get; set; }
    }
}
