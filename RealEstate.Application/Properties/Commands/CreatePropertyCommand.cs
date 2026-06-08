using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;

namespace RealEstate.Application.Properties.Commands;

public record CreatePropertyCommand(
    string Title,
    string Description,
    decimal Price,
    string Address) : IRequest<int>;
