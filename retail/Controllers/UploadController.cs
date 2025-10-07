using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

namespace ABCRetailers.Controllers
{
    public class UploadController : Controller
    {
        // Replaced IAzureStorageService with IFunctionsApi
        private readonly IFunctionsApi _functionsApi;

        public UploadController(IFunctionsApi functionsApi)
        {
            _functionsApi = functionsApi;
        }

        public ActionResult Index()
        {
            // Initializes the view model (assuming FileUploadModel now includes OrderId and CustomerName)
            return View(new FileUploadModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.ProofOfPayment != null && model.ProofOfPayment.Length > 0)
                    {
                        // MIGRATED: Use the API service to handle the file upload. 
                        // The API client serializes the multipart data and sends it to the Azure Function.
                        var fileName = await _functionsApi.UploadProofOfPaymentAsync(
                            model.ProofOfPayment,
                            model.OrderId,
                            model.CustomerName
                        );

                        // The Function handles the actual storage (Blob and File Share) on the backend.
                        TempData["Success"] = $"File uploaded successfully via API! File name: {fileName}";

                        // Clear the model for a fresh form
                        return View(new FileUploadModel());
                    }
                    else
                    {
                        ModelState.AddModelError("ProofOfPayment", "Please select a file to upload.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error uploading file via API: {ex.Message}");
                }
            }
            return View(model);
        }
    }
}
