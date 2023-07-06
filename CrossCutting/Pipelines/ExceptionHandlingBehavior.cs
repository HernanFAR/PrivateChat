using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VSlices.Core.Abstracts.BusinessLogic;
using VSlices.CrossCutting.ExceptionHandling;

namespace CrossCutting.Pipelines;

public class ExceptionHandlingBehavior<TRequest, TResponse> : AbstractExceptionHandlingBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    protected override ValueTask ProcessExceptionAsync(Exception ex)
    {
        return ValueTask.CompletedTask;
    }
}
