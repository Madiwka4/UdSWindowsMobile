using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HelloWorldUWP.Modules.Mensa.Models;

namespace HelloWorldUWP.Modules.Mensa.Services
{
    public class MealsResult
    {
        public List<Meal> Meals { get; set; } = new List<Meal>();
        public bool IsClosed { get; set; }
        public bool IsFromCache { get; set; }
        public string ErrorMessage { get; set; }
    }

    public interface IOpenMensaService
    {
        Task<List<Canteen>> GetCanteensAsync();
        Task<List<CanteenDay>> GetOpenDaysAsync(int canteenId);
        Task<MealsResult> GetMealsAsync(int canteenId, DateTime date, bool forceRefresh = false);
    }
}
