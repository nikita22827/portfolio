using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaReferenceSystem;

public static class DatabaseConfig
{
    public const string ConnectionString =
        "Host=localhost;Port=5432;Username=postgres;Password=1234;Database=cinema_db";
}