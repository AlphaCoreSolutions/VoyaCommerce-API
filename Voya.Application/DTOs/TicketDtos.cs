namespace Voya.Application.DTOs;

public record CreateTicketRequest(
	string Subject,
	string Description,
	string Type,
	string Priority,
	string? OrderId
);

public record TicketDto(
	Guid Id,
	string Subject,
	string Status,
	string CreatedAt,
	string? AdminResponse
);