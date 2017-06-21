using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrivialData
{
    public class DB
    {
        static object _lock = new object();
        static string DATA = @"Data\Trivial.db";
        public static void Clear()
        {
            lock (_lock)
            {
                using (var db = new LiteDatabase(DATA))
                {
                    db.DropCollection("tags");
                }
            }
        }

        public static void RemoveDocumentTags(string documentName)
        {
            lock (_lock)
            {
                // Open database (or create if doesn't exist)
                using (var db = new LiteDatabase(DATA))
                {
                    // Get customer collection
                    var col = db.GetCollection<Tag>("tags");

                    int count = col.Delete(Query.EQ("DocumentName", documentName));
                    col.EnsureIndex(x => x.Name);
                }
            }
        }

        public static void UpsertTag(Tag tag)
        {
            lock (_lock)
            {
                // Open database (or create if doesn't exist)
                using (var db = new LiteDatabase(DATA))
                {
                    // Get customer collection
                    var col = db.GetCollection<Tag>("tags");

                    var match = col.Find(x => x.Id == tag.Id).FirstOrDefault();
                    if (match != null)
                    {
                        match.DateTime = tag.DateTime;
                        match.DocumentName = tag.DocumentName;
                        match.DocumentPath = tag.DocumentPath;
                        match.Line = tag.Line;
                        match.ParentContentType = tag.ParentContentType;
                        match.FollowUps = tag.FollowUps;
                        col.Update(match);
                    }
                    else
                    {
                        // Insert new customer document (Id will be auto-incremented)
                        try
                        {
                            col.Insert(tag);
                        }
                        catch { }
                    }

                    //col.Update(customer);

                    // Index document using a document property
                    col.EnsureIndex(x => x.Name);
                }
            }
        }

        public static void AddTags(IEnumerable<Tag> tags)
        {
            lock (_lock)
            {
                // Open database (or create if doesn't exist)
                using (var db = new LiteDatabase(DATA))
                {
                    // Get customer collection
                    var col = db.GetCollection<Tag>("tags");

                    //// Create your new customer instance
                    //var tag = new Tag
                    //{
                    //    Name = "John Doe",
                    //    Phones = new string[] { "8000-0000", "9000-0000" },
                    //    IsActive = true
                    //};

                    // Insert new customer document (Id will be auto-incremented)
                    foreach (var tag in tags)
                        try
                        {
                            col.Insert(tag);
                        }
                        catch { }


                    //// Update a document inside a collection
                    //customer.Name = "Joana Doe";

                    //col.Update(tag);

                    // Index document using a document property
                    col.EnsureIndex(x => x.Name);
                }
            }
        }

        public static IEnumerable<Tag> GetTags(string name)
        {
            lock (_lock)
            {
                IEnumerable<Tag> result = null;
                using (var db = new LiteDatabase(DATA))
                {
                    // Get customer collection
                    var col = db.GetCollection<Tag>("tags");
                    result = col.Find(Query.EQ("Name", name))
                        .ToList();
                }
                return result;
            }
        }

        public static IEnumerable<Tag> GetTags(List<string> names)
        {
            lock (_lock)
            {
                List<Tag> result = new List<Tag>();
                using (var db = new LiteDatabase(DATA))
                {
                    // Get customer collection
                    var col = db.GetCollection<Tag>("tags");
                    names.ForEach(s => result.AddRange(col.Find(Query.EQ("Name", s))));

                }
                return result;
            }
        }

        public static void UpsertTags(List<Tag> actionTags)
        {
            lock (_lock)
            {
                // Open database (or create if doesn't exist)
                using (var db = new LiteDatabase(DATA))
                {
                    // Get customer collection
                    var col = db.GetCollection<Tag>("tags");

                    foreach (var tag in actionTags)
                    {
                        var match = col.Find(x => x.Id == tag.Id).FirstOrDefault();
                        if (match != null)
                        {
                            match.DateTime = tag.DateTime;
                            match.DocumentName = tag.DocumentName;
                            match.DocumentPath = tag.DocumentPath;
                            match.Line = tag.Line;
                            match.ParentContentType = tag.ParentContentType;
                            match.FollowUps = tag.FollowUps;
                            col.Update(match);
                        }
                        else
                        {
                            // Insert new customer document (Id will be auto-incremented)
                            try
                            {
                                col.Insert(tag);
                            }
                            catch { }
                        }
                    }

                    // Index document using a document property
                    col.EnsureIndex(x => x.Name);
                }
            }
        }
    }
}
