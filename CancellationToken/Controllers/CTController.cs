using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CT.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CTController : ControllerBase
    {
        private readonly ILogger<CTController> _logger;

        public CTController(ILogger<CTController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("GetCT")]
        public IActionResult GetCT(/*CancellationToken ct*/)
        {
            var ct2 = new CancellationTokenSource();
            ct2.CancelAfter(TimeSpan.FromSeconds(5));

            for (long i = 0; i <= 6; i++)
            {
                Thread.Sleep(1000);

                //ct.ThrowIfCancellationRequested();
                ct2.Token.ThrowIfCancellationRequested();
            }

            return Ok("okay");
        }

        [HttpGet]
        [Route("GetCT2")]
        public async Task<IActionResult> GetCTCustom(CancellationToken ct)
        {
            await new CTTest().LongTask(ct);

            return Ok("okay");
        }
    }

    public class CTTest
    {
        public async Task LongTask(CancellationToken ct)
        {
            for (var i = 0; i <= 180; i++)
            {
                await Task.Delay(1000, ct);
                ct.ThrowIfCancellationRequested();
            }
        }
    }

    

    public class TimeoutCancellationTokenModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context?.Metadata.ModelType != typeof(CancellationToken))
            {
                return null;
            }

            return new TimeoutCancellationTokenModelBinder();
        }

        private class TimeoutCancellationTokenModelBinder : CancellationTokenModelBinder, IModelBinder
        {
            public new async Task BindModelAsync(ModelBindingContext bindingContext)
            {
                await base.BindModelAsync(bindingContext);
                if (bindingContext.Result.Model is CancellationToken cancellationToken)
                {
                    // combine the default token with a timeout
                    var timeoutCts = new CancellationTokenSource();
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
                    var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                    // We need to force boxing now, so we can insert the same reference to the boxed CancellationToken
                    // in both the ValidationState and ModelBindingResult.
                    //
                    // DO NOT simplify this code by removing the cast.
                    var model = (object)combinedCts.Token;
                    bindingContext.ValidationState.Clear();
                    bindingContext.ValidationState.Add(model, new ValidationStateEntry() { SuppressValidation = true });
                    bindingContext.Result = ModelBindingResult.Success(model);
                }
            }
        }
    }
}