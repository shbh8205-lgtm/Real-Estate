using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Interfaces;
using BCrypt.Net;

namespace RealEstate.Application.Auth;

public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IClientRepository _clientRepo;
    private readonly IJwtService _jwtService;

    public RegisterHandler(IClientRepository clientRepo, IJwtService jwtService)
    {
        _clientRepo = clientRepo;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        if (await _clientRepo.ExistsAsync(request.Email))
            throw new InvalidOperationException("המשתמש כבר קיים");

        var client = new Client
        {
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            MaxBudget = request.MaxBudget,
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        var created = await _clientRepo.CreateAsync(client);
        var token = _jwtService.GenerateToken(created);

        return new AuthResponse(token, new ClientDto(
            created.Id, created.FullName, created.Email,
            created.PhoneNumber, created.MaxBudget, created.Role.ToString()
        ));
    }
}