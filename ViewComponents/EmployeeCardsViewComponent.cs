using Microsoft.AspNetCore.Mvc;
using Smart_Attendance_System.Services.Interfaces;
using Smart_Attendance_System.Models.ViewModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Smart_Attendance_System.ViewComponents
{
    public class EmployeeCardsViewComponent : ViewComponent
    {
        private readonly IAdminRepository _adminRepository;

        public EmployeeCardsViewComponent(IAdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            IEnumerable<EmployeeVM> employees = await _adminRepository.GetAllEmployeeBasicInfoAsync();
            return View("_EmployeeCards", employees);
        }
    }
}
