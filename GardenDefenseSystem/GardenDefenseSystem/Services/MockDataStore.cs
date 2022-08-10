using GardenDefenseSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GardenDefenseSystem.Services
{
    public class MockDataStore : IDataStore<ObjectDetectedShotLog>
    {
        readonly List<ObjectDetectedShotLog> items;

        public MockDataStore()
        {
            items = new List<ObjectDetectedShotLog>()
            {
                new ObjectDetectedShotLog { Id = Guid.NewGuid().ToString(), Text = "First item", Description="This is an item description." },
               
            };
        }

        public async Task<bool> AddItemAsync(ObjectDetectedShotLog item)
        {
            items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateItemAsync(ObjectDetectedShotLog item)
        {
            var oldItem = items.Where((ObjectDetectedShotLog arg) => arg.Id == item.Id).FirstOrDefault();
            items.Remove(oldItem);
            items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(string id)
        {
            var oldItem = items.Where((ObjectDetectedShotLog arg) => arg.Id == id).FirstOrDefault();
            items.Remove(oldItem);

            return await Task.FromResult(true);
        }

        public async Task<ObjectDetectedShotLog> GetItemAsync(string id)
        {
            return await Task.FromResult(items.FirstOrDefault(s => s.Id == id));
        }

        public async Task<IEnumerable<ObjectDetectedShotLog>> GetItemsAsync(bool forceRefresh = false)
        {
            return await Task.FromResult(items);
        }
    }
}