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
    public class CoverServiceTests
    {
        private readonly Mock<ICoverRepository> _coverRepository = new();
        private readonly Mock<IAuditQueue> _auditQueue = new();
        private readonly Mock<IMapper> _mapper = new();

        private readonly CoverService _sut;

        public CoverServiceTests()
        {
            _sut = new CoverService(
                _coverRepository.Object,
                _auditQueue.Object,
                new CoverValidator(),
                _mapper.Object);
        }

        private static CoverDto ValidCover()
        {
            return new CoverDto
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(31)),
                Type = CoverType.Yacht
            };
        }

        [Fact]
        public async Task CreateAsync_ValidCover_PersistsCoverAndEnqueuesAudit()
        {
            // Arrange
            var dto = ValidCover();

            Cover? savedCover = null;

            _mapper
                .Setup(mapper => mapper.Map<Cover>(It.IsAny<CoverDto>()))
                .Returns((CoverDto source) => new Cover
                {
                    Id = source.Id,
                    StartDate = source.StartDate,
                    EndDate = source.EndDate,
                    Type = source.Type,
                    Premium = source.Premium
                });

            _coverRepository
                .Setup(repository => repository.AddAsync(
                    It.IsAny<Cover>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Cover, CancellationToken>((cover, _) => savedCover = cover)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.CreateAsync(dto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(Guid.TryParse(result.Id, out _));
            Assert.True(result.Premium > 0);
            Assert.NotNull(savedCover);
            Assert.Equal(result.Id, savedCover.Id);
            Assert.Equal(result.Premium, savedCover.Premium);

            _coverRepository.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Cover>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _auditQueue.Verify(queue => queue.Enqueue(
                AuditEntityTypes.Cover,
                result.Id,
                AuditActionType.Create),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_StartDateInPast_ThrowsValidationException()
        {
            // Arrange
            var dto = ValidCover();
            dto.StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
            dto.EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10));

            // Act / Assert
            await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(dto, CancellationToken.None));

            _coverRepository.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Cover>(),
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
        public async Task CreateAsync_EndDateBeforeStartDate_ThrowsValidationException()
        {
            // Arrange
            var dto = ValidCover();
            dto.StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
            dto.EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5));

            // Act / Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _sut.CreateAsync(dto, CancellationToken.None));

            _coverRepository.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Cover>(),
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
        public async Task CreateAsync_EndDateMoreThanOneYearAfterStart_ThrowsValidationException()
        {
            // Arrange
            var dto = ValidCover();
            dto.StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
            dto.EndDate = dto.StartDate.AddYears(1).AddDays(1);

            // Act / Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _sut.CreateAsync(dto, CancellationToken.None));

            _coverRepository.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Cover>(),
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
        public async Task CreateAsync_RepositoryThrows_DoesNotEnqueueAudit()
        {
            // Arrange
            var dto = ValidCover();

            _mapper
                .Setup(mapper => mapper.Map<Cover>(It.IsAny<CoverDto>()))
                .Returns((CoverDto source) => new Cover
                {
                    Id = source.Id,
                    StartDate = source.StartDate,
                    EndDate = source.EndDate,
                    Type = source.Type,
                    Premium = source.Premium
                });

            _coverRepository
                .Setup(repository => repository.AddAsync(
                    It.IsAny<Cover>(),
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
            var covers = new List<Cover>
            {
                new()
                {
                    Id = "cover-1",
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                    Type = CoverType.Yacht,
                    Premium = 1_000m
                },
                new()
                {
                    Id = "cover-2",
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(60)),
                    Type = CoverType.Tanker,
                    Premium = 2_000m
                }
            };

            IReadOnlyList<CoverDto> expected =
            [
                new()
                {
                    Id = "cover-1",
                    StartDate = covers[0].StartDate,
                    EndDate = covers[0].EndDate,
                    Type = covers[0].Type,
                    Premium = covers[0].Premium
                },
                new()
                {
                    Id = "cover-2",
                    StartDate = covers[1].StartDate,
                    EndDate = covers[1].EndDate,
                    Type = covers[1].Type,
                    Premium = covers[1].Premium
                }
            ];

            _coverRepository
                .Setup(repository => repository.GetAllAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(covers);

            _mapper
                .Setup(mapper => mapper.Map<IReadOnlyList<CoverDto>>(covers))
                .Returns(expected);

            // Act
            var result = await _sut.GetAllAsync(CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Same(expected, result);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingCover_ReturnsMappedDto()
        {
            // Arrange
            var entity = new Cover
            {
                Id = "cover-123",
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                Type = CoverType.Yacht,
                Premium = 1_000m
            };

            var expected = new CoverDto
            {
                Id = entity.Id,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Type = entity.Type,
                Premium = entity.Premium
            };

            _coverRepository
                .Setup(repository => repository.GetByIdAsync(
                    entity.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mapper
                .Setup(mapper => mapper.Map<CoverDto>(entity))
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
        public async Task GetByIdAsync_MissingCover_ReturnsNull()
        {
            // Arrange
            _coverRepository
                .Setup(repository => repository.GetByIdAsync(
                    "missing",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Cover?)null);

            _mapper
                .Setup(mapper => mapper.Map<CoverDto>(It.IsAny<Cover?>()))
                .Returns((CoverDto?)null);

            // Act
            var result = await _sut.GetByIdAsync(
                "missing",
                CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_ExistingCover_DeletesAndEnqueuesAudit()
        {
            // Arrange
            const string id = "cover-123";

            _coverRepository
                .Setup(repository => repository.DeleteAsync(
                    id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.DeleteAsync(id, CancellationToken.None);

            // Assert
            Assert.True(result);

            _auditQueue.Verify(queue => queue.Enqueue(
                AuditEntityTypes.Cover,
                id,
                AuditActionType.Delete),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_MissingCover_ReturnsFalseAndDoesNotEnqueueAudit()
        {
            // Arrange
            const string id = "missing";

            _coverRepository
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
            _coverRepository
                .Setup(repository => repository.DeleteAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database failure"));

            // Act / Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.DeleteAsync("cover-123", CancellationToken.None));

            _auditQueue.Verify(queue => queue.Enqueue(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Never);
        }

        [Theory]
        [InlineData(CoverType.Yacht, 1375)]
        [InlineData(CoverType.PassengerShip, 1500)]
        [InlineData(CoverType.Tanker, 1875)]
        public void ComputePremium_OneDay_ReturnsExpectedPremium(
            CoverType coverType,
            decimal expected)
        {
            // Arrange
            var startDate = new DateTime(2027, 1, 1);
            var endDate = startDate.AddDays(1);

            // Act
            var result = _sut.ComputePremium(
                DateOnly.FromDateTime(startDate),
                DateOnly.FromDateTime(endDate),
                coverType);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ComputePremium_DefaultCoverType_UsesDefaultMultiplier()
        {
            // Arrange
            var startDate = new DateTime(2027, 1, 1);
            var endDate = startDate.AddDays(1);

            // Act
            var result = _sut.ComputePremium(
                DateOnly.FromDateTime(startDate),
                DateOnly.FromDateTime(endDate),
                (CoverType)999);

            // Assert
            Assert.Equal(1625m, result);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(30, 41250)]
        [InlineData(31, 42556.250)]
        public void ComputePremium_Yacht_RespectsDurationBoundaries(
            int durationDays,
            decimal expected)
        {
            // Arrange
            var startDate = new DateTime(2027, 1, 1);
            var endDate = startDate.AddDays(durationDays);

            // Act
            var result = _sut.ComputePremium(
                DateOnly.FromDateTime(startDate),
                DateOnly.FromDateTime(endDate),
                CoverType.Yacht);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
