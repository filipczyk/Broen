using EInvoiceBridge.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace EInvoiceBridge.Tests.Integration.Repositories;

public class InvoiceRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public InvoiceRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Stub — requires Flyway migrations applied to test container")]
    public async Task InsertAsync_WithValidData_ReturnsId()
    {
        // TODO: Apply Flyway migrations, then test insert
    }
}
