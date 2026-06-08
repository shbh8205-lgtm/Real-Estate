using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using RealEstate.Domain.Interfaces;
using BCrypt.Net;

namespace RealEstate.Application.Auth;

public class LoginHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IClientRepository _clientRepo;
    private readonly IJwtService _jwtService;

    public LoginHandler(IClientRepository clientRepo, IJwtService jwtService)
    {
        _clientRepo = clientRepo;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var client = await _clientRepo.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("אימייל או סיסמה שגויים");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, client.PasswordHash))
            throw new UnauthorizedAccessException("אימייל או סיסמה שגויים");

        var token = _jwtService.GenerateToken(client);

        return new AuthResponse(token, new ClientDto(
            client.Id, client.FullName, client.Email,
            client.PhoneNumber, client.MaxBudget, client.Role.ToString()
        ));
    }
}