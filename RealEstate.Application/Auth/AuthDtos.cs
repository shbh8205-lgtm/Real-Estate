using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealEstate.Domain.Entities;
using MediatR;

namespace RealEstate.Application.Auth;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
public record RegisterCommand(
    string FullName,
    string Email,
    string Password,
    string PhoneNumber,
    decimal MaxBudget,
    ClientRole Role
) : IRequest<AuthResponse>;

public record AuthResponse(string Token, ClientDto Client);
public record ClientDto(
    int Id,
    string FullName,
    string Email,
    string PhoneNumber,
    decimal MaxBudget,
    string Role
);