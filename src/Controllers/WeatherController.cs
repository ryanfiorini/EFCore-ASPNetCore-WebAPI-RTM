using System;
using System.Collections.Generic;
using System.Linq;
using EFCoreWebAPI.Data;
using EFCoreWebAPI.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EFCoreWebAPI.Internal;


namespace EFCoreWebAPI.Controllers
{
    [Route("api/[controller]")]
    public class WeatherController : Controller
    {
        WeatherContext _context;

        WeatherDataRepository _repo;


        public WeatherController(WeatherContext context, WeatherDataRepository services)
        {
            _context = context;
            _repo = services;
        }


        [HttpGet]
        public IEnumerable<WeatherEvent> Get()
        {
            return _context.WeatherEvents.Include(w => w.Reactions).ToList();

        }


        //api/Weather/2016-01-28
        [HttpGet("{date}")]
        public IEnumerable<WeatherEvent> Get(DateTime date)
        {
            return _context.WeatherEvents.Where(w => w.Date.Date == date.Date).ToList();
        }

        //api/Weather/1
        [HttpGet("{weatherType:int}")]
        public IEnumerable<WeatherEvent> Get(int weatherType)
        {
            //return _context.WeatherEvents.FromSql($"SELECT * FROM EventsByType({weatherType})").OrderByDescending(e => e.Id);
            // return _context.WeatherEvents.Where(w => w.Type==WeatherType.Rain).ToList();
            return _context.WeatherEvents.Where(w => (int)w.Type == weatherType).ToList();
        }

        [HttpPost]
        public int LogWeatherEvent(DateTime datetime, WeatherType type, bool happy,
                                        string name, string quote)
        {
            
            //This controller method demonstrates that DbSet Add behaves the same
            //as EF6: affecting every item in a [disconnected] graph with the same state
            //As well as Attach, Remove and the new Update.
            
            WeatherEvent wE;
            if (String.IsNullOrEmpty(name))
            {
                wE = WeatherEvent.Create(datetime, type, happy);
            }
            else
            {
                wE = WeatherEvent.Create(datetime, type, happy,
                                         new List<string[]> { new[] { name, quote } });
            }
             _context.WeatherEvents.Add(wE);
            var affectedRowCount = _context.SaveChanges();
            return affectedRowCount;
        }

        [HttpPut("{eventId}")]
        public string GetAndUpdateMostCommonWord(int eventId)
        {
            //This contoller method demonstrates the NEW BEHAVIOR of DbContext.Entry.State
            //which will only affect the root of the graph passed in
            var eventGraph = _repo.GetWeatherEventAndReactionsById(eventId);
            var theWord = ReactionParser.MostFrequentWord(
                eventGraph.Reactions.Select(r => r.Quote).ToList());
            eventGraph.MostCommonWord = theWord;
            _repo.UpdateWeatherEventOnly(eventGraph);
            Console.WriteLine($"NOTE: Graph still has {eventGraph.Reactions.Count()} reactions attached");
            return theWord;
        }

        [HttpPutAttribute("clean")]
        public string FixSeasonInReactions()
        { 
             //This contoller method demonstrates the NEW DbContext.ChangeTracker.TrackGraph method
             //(in the repo method that's called from here)
            //which will iterate through all items in a graph and apply the specified 
            //function each item. 
   
            int totalDbRowsUpdated = 0;
            var eventGraphs = _repo.GetAllEventsWithReactions();
            foreach (var graph in eventGraphs.Where(e => e.Date.Month > 4))
            {
                foreach (var reaction in graph.Reactions)

                {
                    if (reaction.Quote.ToUpper().Contains("SNOW"))
                    {
                        reaction.Quote = reaction.Quote.ToUpper().Replace("SNOW", "sun");
                        reaction.ObjectState = ObjectState.Modified;
                    }

                }
                var affectedRecords = _repo.UpdateRandomStateWeatherEventGraphs(eventGraphs);
                totalDbRowsUpdated += affectedRecords;
            }
            return totalDbRowsUpdated.ToString();
             }



    }
}


