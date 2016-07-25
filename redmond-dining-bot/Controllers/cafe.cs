﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using msftbot.Support;
using Newtonsoft.Json;

namespace msftbot
{
    public class Cafe
    {
        public int CafeId { get; set; }
        public string CafeName { get; set; }
        public string CafeHours { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string StateName { get; set; }
        public int ZipCode { get; set; }
        public string Phone { get; set; }
        public string Campus { get; set; }
        public int BuildingId { get; set; }
        public string PictureURL { get; set; }
        public bool EspressoAvailable { get; set; }
    }

    public class CafeMenu
    {
        public string Name { get; set; }
        public Cafeitem[] CafeItems { get; set; }
        public int CafeId { get; set; }
        public string CafeName { get; set; }
        public int Id { get; set; }
    }

    public class Cafeitem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Prices { get; set; }
        public string[] WeekDays { get; set; }
        public int Id { get; set; }
    }

    internal class CafeActions
    {
        internal CafeActions()
        { }

        internal async Task<string> GetAllCafes()
        {
            // Get authentication token from authentication.cs
            Authentication auth = new Authentication();
            string authtoken = await auth.GetAuthHeader();

            // Get JSON – List of all Cafes
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Constants.AuthHeaderValueScheme, authtoken);
            HttpResponseMessage response = await httpClient.GetAsync(Constants.listAllCafeNames);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // Convert JSON to list
            List<Cafe> allCafeList = JsonConvert.DeserializeObject<List<Cafe>>(responseBody);

            // Format list
            StringBuilder allcafes = new StringBuilder();
            allCafeList.ForEach(i => {
                allcafes.AppendFormat(Constants.cafeListFormat, i.CafeName, Constants.singleCafeMenuApi, Environment.NewLine);
            });

            return allcafes.ToString();
        }

        internal async Task<string> GetCafeForItem(string dining)
        {
            // Get authentication token from authentication.cs
            Authentication auth = new Authentication();
            string authtoken = await auth.GetAuthHeader();

            // Get JSON – List of all Cafe serving the requested item
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Constants.AuthHeaderValueScheme, authtoken);
            HttpResponseMessage response = await httpClient.GetAsync(string.Format(Constants.listCafesServingItem, dining));
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // Convert JSON to list
            List<Cafe> list = JsonConvert.DeserializeObject<List<Cafe>>(responseBody);

            // Format list
            StringBuilder cafe = new StringBuilder();
            list.ForEach(i =>
            {
                cafe.AppendFormat(Constants.cafeListFormat, i.CafeName, Constants.singleCafeMenuApi, Environment.NewLine);
            });

            return cafe.ToString();
        }

        internal async Task<string> GetCafeMenu(string location)
        {
            //do this first to avoid lots of extra processing.
            // Get the day of the week (1 – 5) for use in API URI. 
            DateTime day = DateTime.Now;
            int today = (int)day.DayOfWeek;

            // String menu - empty string will be populating from json response.
            StringBuilder menu = new StringBuilder();

            if ((day.DayOfWeek == DayOfWeek.Saturday) || (day.DayOfWeek == DayOfWeek.Sunday))
            {
                menu.AppendLine(Constants.cafeNotOpenWeekendDialogue);
                return menu.ToString();
            }


            // Get authentication token from authentication.cs
            Authentication auth = new Authentication();
            string authtoken = await auth.GetAuthHeader();

            // Get JSON – List of all Cafes
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Constants.AuthHeaderValueScheme, authtoken);
            HttpResponseMessage ResponseAllCafe = await httpClient.GetAsync(Constants.listAllCafeNames);
            ResponseAllCafe.EnsureSuccessStatusCode();
            string RespnseBodyAllCafe = await ResponseAllCafe.Content.ReadAsStringAsync();

            // Convert JSON to list
            List<Cafe> allCafeList = JsonConvert.DeserializeObject<List<Cafe>>(RespnseBodyAllCafe);

            //Formatting for API call
            if (location.Contains(Constants.buildingEntity))
            {
                location = location.Replace(Constants.buildingEntity, Constants.cafeEntity);
            }
            if (!location.Contains(Constants.cafeEntity))
            {
                // if no cafe already in location add "cafe". Explicitely calling this out to handle location = "36"
                location = Constants.cafeEntity + location;
            }

            var buildingid =
                from n in allCafeList
                where n.CafeName.Equals(location, StringComparison.OrdinalIgnoreCase)
                select n;

            string newid = string.Empty;

            foreach (Cafe item in buildingid)
            {
                newid = item.BuildingId.ToString();
            }

            try
            {

                //Get JSON – Cafe menu
                HttpResponseMessage response = await httpClient.GetAsync(string.Format(Constants.listCafeMenu, newid, today));
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                // Convert JSON to list
                List<CafeMenu> list = JsonConvert.DeserializeObject<List<CafeMenu>>(responseBody);

                // Format header – URL to café menu of dining site
                menu.AppendFormat(Constants.linkToCafeMenuFormat, location, Constants.dinningMenuWebsiteUrl, Environment.NewLine);

                // Populate string with menu item description - convert to LINQ query
                list.ForEach(i =>
                {
                    menu.AppendFormat(Constants.menuItemLocationFormat, i.Name, Environment.NewLine);
                    i.CafeItems.ToList().ForEach(ci => menu.AppendFormat(Constants.menuItemTypeFormat, ci.Name, Environment.NewLine));
                });

            }
            catch
            {
                menu.AppendLine(Constants.noMenuFoundDialogue);
            }
            // Return list
            return menu.ToString();
        }
    }
}