﻿using ComicBookLibraryManager.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;

namespace ComicBookLibraryManager.Data
{
    /// <summary>
    /// Repository class that provides various database queries
    /// and CRUD operations.
    /// </summary>
    public static class Repository
    {
        /// <summary>
        /// Private method that returns a database context.
        /// </summary>
        /// <returns>An instance of the Context class.</returns>
        static Context GetContext()
        {
            var context = new Context();
            context.Database.Log = (message) => Debug.WriteLine(message);
            return context;
        }

        /// <summary>
        /// Returns a count of the comic books.
        /// </summary>
        /// <returns>An integer count of the comic books.</returns>
        public static int GetComicBookCount()
        {
            using (Context context = GetContext())
            {
                return context.ComicBooks.Count();
            }
        }

        /// <summary>
        /// Returns a list of comic books ordered by the series title 
        /// and issue number.
        /// </summary>
        /// <returns>An IList collection of ComicBook entity instances.</returns>
        public static IList<ComicBook> GetComicBooks()
        {
            using (Context context = GetContext())
            {
                return context.ComicBooks
                    .Include(cb => cb.Series)
                    .OrderBy(cb => cb.Series.Title)
                    .ThenBy(cb => cb.IssueNumber)
                    .ToList();
            }
        }

        /// <summary>
        /// Returns a single comic book.
        /// </summary>
        /// <param name="comicBookId">The comic book ID to retrieve.</param>
        /// <returns>A fully populated ComicBook entity instance.</returns>
        public static ComicBook GetComicBook(int comicBookId)
        {
            using (Context context = GetContext())
            {
                return context.ComicBooks
                    .Include(cb => cb.Series)
                    .Include(cb => cb.Artists.Select(a => a.Artist))
                    .Include(cb => cb.Artists.Select(a => a.Role))
                    .Where(cb => cb.Id == comicBookId)
                    .SingleOrDefault();
            }
        }

        /// <summary>
        /// Returns a list of series ordered by title.
        /// </summary>
        /// <returns>An IList collection of Series entity instances.</returns>
        public static IList<Series> GetSeries()
        {
            using (Context context = GetContext())
            {
                return context.Series
                    .OrderBy(s => s.Title)
                    .ToList();
            }
        }

        /// <summary>
        /// Returns a single series.
        /// </summary>
        /// <param name="seriesId">The series ID to retrieve.</param>
        /// <returns>A Series entity instance.</returns>
        public static Series GetSeries(int seriesId)
        {
            using (Context context = GetContext())
            {
                return context.Series
                    .Where(s => s.Id == seriesId)
                    .SingleOrDefault();
            }
        }

        /// <summary>
        /// Returns a list of artists ordered by name.
        /// </summary>
        /// <returns>An IList collection of Artist entity instances.</returns>
        public static IList<Artist> GetArtists()
        {
            using (Context context = GetContext())
            {
                return context.Artists
                    .OrderBy(a => a.Name)
                    .ToList();
            }
        }

        /// <summary>
        /// Returns a list of roles ordered by name.
        /// </summary>
        /// <returns>An IList collection of Role entity instances.</returns>
        public static IList<Role> GetRoles()
        {
            using (Context context = GetContext())
            {
                return context.Roles
                    .OrderBy(r => r.Name)
                    .ToList();
            }
        }

        /// <summary>
        /// Adds a comic book.
        /// </summary>
        /// <param name="comicBook">The ComicBook entity instance to add.</param>
        public static void AddComicBook(ComicBook comicBook)
        {
            using (Context context = GetContext())
            {
                context.ComicBooks.Add(comicBook);

                // Prevent Series from being re-saved to the database (changes Entry's
                // state from "Added" to "Unchanged")
                if (comicBook.Series != null && comicBook.Series.Id > 0)
                {
                    context.Entry(comicBook.Series).State = EntityState.Unchanged;
                }

                // Prevent Artists collection (and its related object, Role)
                // from being re-saved to the database (changes Entry's state
                // from "Added" to "Unchanged")
                foreach (var artist in comicBook.Artists)
                {
                    if (artist.Artist != null && artist.Artist.Id > 0)
                    {
                        context.Entry(artist.Artist).State = EntityState.Unchanged;
                    }

                    if (artist.Role != null && artist.Role.Id > 0)
                    {
                        context.Entry(artist.Role).State = EntityState.Unchanged;
                    }
                }

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Updates a comic book.
        /// </summary>
        /// <param name="comicBook">The ComicBook entity instance to update.</param>
        public static void UpdateComicBook(ComicBook comicBook)
        {
            using (Context context = GetContext())
            {
                // Retrieve comic book from the current context OR the database
                ComicBook comicBookToUpdate = context.ComicBooks.Find(comicBook.Id);

                // OPTION 1: Set each passed-in comic book field value on the new comic book instance
                /*comicBookToUpdate.SeriesId = comicBook.SeriesId;
                comicBookToUpdate.IssueNumber = comicBook.IssueNumber;
                comicBookToUpdate.Description = comicBook.Description;
                comicBookToUpdate.PublishedOn = comicBook.PublishedOn;
                comicBookToUpdate.AverageRating = comicBook.AverageRating;*/

                // OPTION 2: Get entry (from context or DB) and set the passed-in
                // comic book's values on that entry.
                // Entity State will be set to "Modified".
                //context.Entry(comicBookToUpdate).CurrentValues.SetValues(comicBook);

                // OPTION 3: Attach passed-in argument to the current context.
                    // This will reduce the number of queries as the first two methods
                    // above require a SELECT and an UPDATE query to execute updates.
                    // Entity Framework cannot detect if actual updates were made to the context.
                    // Entity state will be set to "Unchanged".
                    // You MUST set the entity state to "Modified" to persist changes to the database.
                context.ComicBooks.Attach(comicBook);
                var comicBookEntry = context.Entry(comicBook);
                comicBookEntry.State = EntityState.Modified;

                // prevents the IssueNumber field from being included in the UPDATE statement
                //comicBookEntry.Property("IssueNumber").IsModified = false;

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Deletes a comic book.
        /// </summary>
        /// <param name="comicBookId">The comic book ID to delete.</param>
        public static void DeleteComicBook(int comicBookId)
        {
            using (Context context = GetContext())
            {
                // OPTION 1: Retrieve entry (from context or DB) and remove it from DbSet.
                //ComicBook comicBook = context.ComicBooks.Find(comicBookId);
                //context.ComicBooks.Remove(comicBook);

                // OPTION 2: Use stub entity since we only need the ID to remove a record.
                var comicBook = new ComicBook()
                {
                    Id = comicBookId
                };

                context.Entry(comicBook).State = EntityState.Deleted;

                context.SaveChanges();
            }
        }
    }
}
