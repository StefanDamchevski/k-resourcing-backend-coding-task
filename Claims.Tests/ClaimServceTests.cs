using AutoMapper;
using Claims.Application.Dto;
using Claims.Application.Interfaces;
using Claims.Application.Services;
using Claims.Application.Validation;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using FluentValidation;
using Moq;
using Xunit;

namespace Claims.Tests
{
    public class ClaimServiceTests
    {
        private readonly Mock<IClaimRepository> _claimRepository = new();
        private readonly Mock<ICoverRepository> _coverRepository = new();
        private readonly Mock<IAuditQueue> _auditQueue = new();
        private readonly Mock<IMapper> _mapper = new();

        private readonly ClaimService _sut;

        public ClaimServiceTests()
        {
            var validator = new ClaimValidator(_coverRepository.Object);

            _sut = new ClaimService(
                _claimRepository.Object,
                _auditQueue.Object,
                validator,
                _mapper.Object);
        }

        private static ClaimDto ValidClaim(string coverId = "cover-123")
        {
            return new ClaimDto
            {
                CoverId = coverId,
                Created = DateOnly.FromDateTime(DateTime.Today),
                Name = "Test claim",
                Type = (ClaimType)0,
                DamageCost = 5_000m
            };
        }

        private void ConfigureExistingCover(string coverId, DateTime? startDate = null, DateTime? endDate = null)
        {
            _coverRepository
                .Setup(repository => repository.GetByIdAsync(
                    coverId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Cover
                {
                    Id = coverId,
                    StartDate = DateOnly.FromDateTime(startDate ?? DateTime.Today.AddDays(-30)),
                    EndDate = DateOnly.FromDateTime(endDate ?? DateTime.Today.AddDays(30)),
                    Type = CoverType.Yacht,
                    Premium = 1_000m
                });
        }

        [Fact]
        public async Task CreateAsync_ValidClaim_PersistsClaimAndEnqueuesAudit()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var dto = ValidClaim();
            ConfigureExistingCover(dto.CoverId);

            Claim? savedClaim = null;

            _mapper
                .Setup(mapper => mapper.Map<Claim>(It.IsAny<ClaimDto>()))
                .Returns((ClaimDto source) => new Claim
                {
                    Id = source.Id,
                    CoverId = source.CoverId,
                    Created = source.Created,
                    Name = source.Name,
                    Type = source.Type,
                    DamageCost = source.DamageCost
                });

            _claimRepository
                .Setup(repository => repository.AddAsync(
                    It.IsAny<Claim>(),
                    cancellationToken))
                .Callback<Claim, CancellationToken>((claim, _) => savedClaim = claim)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.CreateAsync(dto, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.Id));
            Assert.Equal(dto.CoverId, result.CoverId);
            Assert.NotNull(savedClaim);
            Assert.Equal(result.Id, savedClaim.Id);

            _claimRepository.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Claim>(),
                    cancellationToken),
                Times.Once);

            _auditQueue.Verify(queue => queue.Enqueue(
                AuditEntityTypes.Claim,
                result.Id,
                AuditActionType.Create),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ValidClaim_GeneratesNewId()
        {
            // Arrange
            var dto = ValidClaim();
            dto.Id = null!;

            ConfigureExistingCover(dto.CoverId);

            _mapper
                .Setup(mapper => mapper.Map<Claim>(It.IsAny<ClaimDto>()))
                .Returns((ClaimDto source) => new Claim
                {
                    Id = source.Id,
                    CoverId = source.CoverId,
                    Created = source.Created,
                    Name = source.Name,
                    Type = source.Type,
                    DamageCost = source.DamageCost
                });

            _claimRepository
                .Setup(repository => repository.AddAsync(
                    It.IsAny<Claim>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _sut.CreateAsync(dto, CancellationToken.None);

            // Assert
            Assert.True(Guid.TryParse(dto.Id, out _));
        }

        [Fact]
        public async Task CreateAsync_EmptyCoverId_ThrowsValidationException()
        {
            // Arrange
            var dto = ValidClaim("");

            // Act / Assert
            await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(dto, CancellationToken.None));

            _claimRepository.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Claim>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _auditQueue.Verify(
                queue => queue.Enqueue(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_CoverDoesNotExist_ThrowsValidationException()
        {
            // Arrange
            var dto = ValidClaim();

            _coverRepository
                .Setup(repository => repository.GetByIdAsync(
                    dto.CoverId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Cover?)null);

            // Act / Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _sut.CreateAsync(dto, CancellationToken.None));

            _claimRepository.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Claim>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _auditQueue.Verify(
                queue => queue.Enqueue(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Theory]
        [InlineData(100001)]
        [InlineData(150000)]
        public async Task CreateAsync_DamageCostExceedsLimit_ThrowsValidationException(
            decimal damageCost)
        {
            // Arrange
            var dto = ValidClaim();
            dto.DamageCost = damageCost;

            ConfigureExistingCover(dto.CoverId);

            // Act / Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _sut.CreateAsync(dto, CancellationToken.None));

            _claimRepository.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Claim>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(31)]
        public async Task CreateAsync_ClaimDateOutsideCoverPeriod_ThrowsValidationException(
            int dayOffset)
        {
            // Arrange
            var dto = ValidClaim();
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(30);

            dto.Created = dayOffset < 0
                ? DateOnly.FromDateTime(startDate.AddDays(dayOffset))
                : DateOnly.FromDateTime(endDate.AddDays(dayOffset - 30));

            ConfigureExistingCover(
                dto.CoverId,
                startDate,
                endDate);

            // Act / Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _sut.CreateAsync(dto, CancellationToken.None));

            _claimRepository.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Claim>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_RepositoryThrows_DoesNotEnqueueAudit()
        {
            // Arrange
            var dto = ValidClaim();
            ConfigureExistingCover(dto.CoverId);

            _mapper
                .Setup(mapper => mapper.Map<Claim>(It.IsAny<ClaimDto>()))
                .Returns((ClaimDto source) => new Claim
                {
                    Id = source.Id,
                    CoverId = source.CoverId,
                    Created = source.Created,
                    Name = source.Name,
                    Type = source.Type,
                    DamageCost = source.DamageCost
                });

            _claimRepository
                .Setup(repository => repository.AddAsync(
                    It.IsAny<Claim>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database failure"));

            // Act / Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.CreateAsync(dto, CancellationToken.None));

            _auditQueue.Verify(
                queue => queue.Enqueue(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GetAllAsync_MapsRepositoryResultsToDtos()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new()
                {
                    Id = "claim-1",
                    CoverId = "cover-1",
                    Created = DateOnly.FromDateTime(DateTime.Today),
                    Name = "Claim one",
                    Type = (ClaimType)0,
                    DamageCost = 1_000m
                },
                new()
                {
                    Id = "claim-2",
                    CoverId = "cover-2",
                    Created = DateOnly.FromDateTime(DateTime.Today),
                    Name = "Claim two",
                    Type = (ClaimType)0,
                    DamageCost = 2_000m
                }
            };

            IReadOnlyList<ClaimDto> expected =
            [
                ValidClaim("cover-1"),
                ValidClaim("cover-2")
            ];

            _claimRepository
                .Setup(repository => repository.GetAllAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(claims);

            _mapper
                .Setup(mapper => mapper.Map<IReadOnlyList<ClaimDto>>(claims))
                .Returns(expected);

            // Act
            var result = await _sut.GetAllAsync(CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Same(expected, result);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingClaim_ReturnsMappedDto()
        {
            // Arrange
            var entity = new Claim
            {
                Id = "claim-123",
                CoverId = "cover-123",
                Created = DateOnly.FromDateTime(DateTime.Today),
                Name = "Test claim",
                Type = (ClaimType)0,
                DamageCost = 5_000m
            };

            var expected = ValidClaim();
            expected.Id = entity.Id;

            _claimRepository
                .Setup(repository => repository.GetByIdAsync(
                    entity.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mapper
                .Setup(mapper => mapper.Map<ClaimDto>(entity))
                .Returns(expected);

            // Act
            var result = await _sut.GetByIdAsync(
                entity.Id,
                CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(entity.Id, result.Id);
            Assert.Same(expected, result);
        }

        [Fact]
        public async Task GetByIdAsync_MissingClaim_ReturnsNull()
        {
            // Arrange
            _claimRepository
                .Setup(repository => repository.GetByIdAsync(
                    "missing",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Claim?)null);

            _mapper
                .Setup(mapper => mapper.Map<ClaimDto>(It.IsAny<Claim?>()))
                .Returns((ClaimDto?)null);

            // Act
            var result = await _sut.GetByIdAsync(
                "missing",
                CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_ExistingClaim_DeletesAndEnqueuesAudit()
        {
            // Arrange
            const string id = "claim-123";

            _claimRepository
                .Setup(repository => repository.DeleteAsync(
                    id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.DeleteAsync(id, CancellationToken.None);

            // Assert
            Assert.True(result);

            _auditQueue.Verify(queue => queue.Enqueue(AuditEntityTypes.Claim, id, AuditActionType.Delete), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_MissingClaim_ReturnsFalseAndDoesNotEnqueueAudit()
        {
            // Arrange
            const string id = "missing";

            _claimRepository
                .Setup(repository => repository.DeleteAsync(
                    id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _sut.DeleteAsync(id, CancellationToken.None);

            // Assert
            Assert.False(result);

            _auditQueue.Verify(queue => queue.Enqueue(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_RepositoryThrows_DoesNotEnqueueAudit()
        {
            // Arrange
            _claimRepository
                .Setup(repository => repository.DeleteAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database failure"));

            // Act / Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.DeleteAsync("claim-123", CancellationToken.None));

            _auditQueue.Verify(queue => queue.Enqueue(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Never);
        }
    }
}