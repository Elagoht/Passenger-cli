using System.Text.Json;

namespace Passenger
{
  /// <summary>
  /// Main work handler
  /// </summary>
  /// <param name="args"></param>
  /// <remarks>
  /// This class is responsible for handling the main work of the program.
  /// </remarks>
  public class Worker(string[] args)
  {
    private readonly Authorization authorization = new(EnDeCoder.JSWSecret);
    private readonly string[] arguments = args.Skip(1).ToArray();

    /// <summary>
    /// Verb control routine
    /// </summary>
    /// <remarks>
    /// This method is responsible for controlling the verbs and their respective methods.
    /// </remarks>
    private void RoutineAuthControl(string verbName, int requiredArgs)
    {
      if (arguments.Length != requiredArgs) Error.ArgumentCount(verbName, requiredArgs);
      if (!authorization.ValidateToken(arguments[0])) Error.InvalidToken();
    }

    /*
     * Authorization
     */

    /// <summary>
    /// Login
    /// </summary>
    /// <remarks>
    /// Generate a JWT token to use other commands. Requires a passphrase.
    /// </remarks>
    public void Login()
    {
      if (arguments.Length != 2) Error.ArgumentCount("login", 2);
      Console.WriteLine(authorization.GenerateToken(arguments[0], arguments[1]));
      Environment.Exit(0);
    }

    /// <summary>
    /// Register
    /// </summary>
    /// <remarks>
    /// Register a passphrase to database.
    /// </remarks>
    public void Register()
    {
      if (arguments.Length != 2) Error.ArgumentCount("register", 2);
      if (Database.IsRegistered())
        Console.WriteLine("passenger: already registered");
      else
        Database.Register(arguments[0], arguments[1]);
    }

    /// <summary>
    /// Reset passphrase for accessing database
    /// </summary>
    /// <remarks>
    /// Reset the passphrase of the Passenger client using a JWT token and a new passphrase.
    /// </remarks>
    public void Reset()
    {
      RoutineAuthControl("reset", 2);
      Database.ResetPassphrase(arguments[1]);
    }

    /*
     * CRUD operations
     */

    /// <summary>
    /// Create a new entry
    /// </summary>
    /// <param name="entry"></param>
    /// <remarks>
    /// Store an entry with the given data, requires a JWT token.
    /// </remarks>
    public void Create()
    {
      RoutineAuthControl("create", 2);
      DatabaseEntry entry = Validate.JsonAsDatabaseEntry(arguments[1]);
      Validate.Entry(entry);
      entry.Created = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
      Console.WriteLine(Database.Create(entry));
    }

    /// <summary>
    /// Fetch all entries without passphrases
    /// </summary>
    /// <returns>A list of all entries</returns>
    /// <remarks>
    /// List all entries without displaying their passphrases, requires a JWT token.
    /// </remarks>
    public void FetchAll()
    {
      RoutineAuthControl("fetchAll", 1);
      Console.WriteLine(
        JsonSerializer.Serialize(Database.FetchAll())
      );
    }

    /// <summary>
    /// Fetch one entry by UUID
    /// </summary>
    /// <returns>DatabaseEntry</returns>
    /// <remarks>
    /// Retrieve an entry by its UUID and increment its total accesses, requires a JWT token.
    /// </remarks>
    public void Fetch()
    {
      RoutineAuthControl("fetch", 2);
      DatabaseEntry entry = Database.FetchOne(arguments[1]);
      if (entry == null) Error.EntryNotFound();
      entry.TotalAccesses++;
      Database.Update(entry.Id, entry, false);
      Console.WriteLine(JsonSerializer.Serialize(entry));
    }

    /// <summary>
    /// Query entries by keyword
    /// </summary>
    /// <returns>A list of entries</returns>
    /// <remarks>
    /// Search for a keyword in all entries, requires a JWT token.
    /// </remarks>
    public void Query()
    {
      RoutineAuthControl("query", 2);
      Console.WriteLine(
        JsonSerializer.Serialize(
          Database.Query(arguments[1])
        )
      );
    }

    /// <summary>
    /// Update an entry
    /// </summary>
    /// <remarks>
    /// Update an entry by its UUID, requires a JWT token and JSON formatted data.
    /// </remarks>
    public void Update()
    {
      RoutineAuthControl("update", 3);
      DatabaseEntry entry = Validate.JsonAsDatabaseEntry(arguments[2]);
      Validate.Entry(entry);
      Database.Update(arguments[1], entry);
    }

    /// <summary>
    /// Delete an entry
    /// </summary>
    /// <remarks>
    /// Delete an entry by its UUID, requires a JWT token.
    /// </remarks>
    public void Delete()
    {
      RoutineAuthControl("delete", 2);
      Database.Delete(arguments[1]);
    }

    /*
     * Statistics
     */

    /// <summary>
    /// Show statistics
    /// </summary>
    /// <remarks>
    /// Show statistics of the database.
    /// </remarks>
    public void Statistics()
    {
      RoutineAuthControl("statistics", 1);
      Statistics statistics = new(Database.AllEntries);
      DashboardData dashboardData = new()
      {
        TotalCount = statistics.TotalCount,
        UniquePlatforms = statistics.UniquePlatforms,
        UniquePlatformsCount = statistics.UniquePlatformsCount,
        UniquePassphrases = statistics.UniquePassphrases,
        MostAccessed = statistics.MostAccessed,
        CommonByPlatform = statistics.CommonByPlatform,
        AverageLength = statistics.AverageLength,
        PercentageOfCommon = statistics.PercentageOfCommon,
        MostCommon = statistics.MostCommon,
        Strengths = statistics.Strengths,
        AverageStrength = statistics.AverageStrength,
        WeakPassphrases = statistics.WeakPassphrases,
        MediumPassphrases = statistics.MediumPassphrases,
        StrongPassphrases = statistics.StrongPassphrases
      };
      Console.WriteLine(JsonSerializer.Serialize(dashboardData));
    }

    /*
     * Constant pairs
     */

    /// <summary> 
    /// Declare a new key-value pair constant
    /// </summary>
    public void Declare()
    {
      RoutineAuthControl("declare", 3);
      ConstantPair constantPair = new()
      {
        Key = arguments[1],
        Value = arguments[2]
      };
      Validate.ConstantPair(constantPair);
      Database.DeclareConstant(constantPair);
    }

    /// <summary>
    /// Forget a key-value pair constant
    /// </summary>
    public void Forget()
    {
      RoutineAuthControl("forget", 2);
      Database.ForgetConstant(arguments[1]);
    }

    /// <summary>
    /// List all declared constants
    /// </summary>
    public void Constants()
    {
      RoutineAuthControl("constants", 1);
      Console.WriteLine(
        JsonSerializer.Serialize(Database.AllConstants)
      );
    }

    /*
     * Generation
     */

    /// <summary>
    /// Generate a passphrase
    /// </summary>
    /// <returns>A secure random passphrase</returns>
    /// <remarks>
    /// Generate a passphrase with the given length. Default length is 32.
    /// </remarks>
    public void Generate()
    {
      switch (arguments.Length)
      {
        case 0:
          Console.WriteLine(Generator.New()); break;
        case 1:
          Console.WriteLine(Generator.New(int.Parse(arguments[0]))); break;
        default:
          Error.ArgumentCount("generate", 0, 1);
          break;
      }
    }

    /// <summary>
    /// Manipulate a passphrase
    /// </summary>
    /// <returns>A manipulated passphrase</returns>
    /// <remarks>
    /// Manipulate a passphrase by changing its characters. Still recognizable by humans.
    /// </remarks>
    public void Manipulate()
    {
      if (arguments.Length != 1) Error.ArgumentCount("manipulate", 1);
      Console.WriteLine(Generator.Manipulated(arguments[0]));
    }

    /*
     * Help and manual
     */

    /// <summary>
    /// Show manual
    /// </summary>
    /// <remarks>
    /// Manual page with UNIX style, plain text to support Windows.
    /// </remarks>
    public static void Manual()
    {
      Console.WriteLine($@"PASSENGER(1)                 Passenger CLI Manual                 PASSENGER(1)

NAME
      Passenger - Portable and customizable password manager.

SYNOPSIS
      passenger [command] [*args]

DESCRIPTION
      Passenger is a command line password manager designed to be portable
      and customizable. It allows users to securely store, retrieve, manage,
      and generate passphrases using their own encode/decode algorithm,
      created from the open-source EnDeCoder.cs file. Each build of the
      Passenger client is unique, crafted by the user, ensuring a
      personalized security algorithm.

COMMANDS
      login -l
            Generate a JWT token to use other commands. Requires a
            passphrase.
            passenger login -l [username] [passphrase]

      register -r
            Register a passphrase to the Passenger client.
            passenger register [username] [passphrase]

      reset -R
            Reset the passphrase of the Passenger client using a JWT token
            and a new passphrase.
            passenger reset [jwt] [new]

      fetchAll -a
            List all entries without displaying their passphrases, requires
            a JWT token.
            passenger fetchAll [jwt]

      query -q
            Search for a keyword in all entries, requires a JWT token.
            passenger query [jwt] [keyword]

      fetch -f
            Retrieve an entry by its UUID, requires a JWT token.
            passenger fetch [jwt] [uuid]

      create -c
            Store an entry with the given json, requires a JWT token.
            passenger create [jwt] [json]

      update -u
            Update an entry by its UUID, requires a JWT token and JSON
            formatted json.
            passenger update [jwt] [uuid] [json]

      delete -d
            Delete an entry by its UUID, requires a JWT token.
            passenger delete [jwt] [uuid]

      stats -s
            Show statistics of the database.
            passenger statis [jwt]

      declare -D
            Declare a new key-value pair, requires a JWT token.
            Theses pairs are constant values that can be replaced
            on response.
            passenger declare [jwt] [key] [value]

      forget -F
            Forget a key-value pair, requires a JWT token.
            passenger forget [jwt] [key]

      constants -C
            List all declared constants, requires a JWT token.
            passenger constants [jwt]

      generate -g
            Generate a passphrase with the given length.
            Default length is 32.
            passenger generate [length]

      manipulate -m
            Manipulate a passphrase by changing its characters.
            Still recognizable by humans.
            passenger manipulate [passphrase]

      version -v --version
            Show the version of the Passenger software.
            passenger version

      help -h --help
            Show this help message and exit.
            passenger help

      man -M
            Show the manual page, if available.
            passenger man

AUTHOR
      Written by Elagoht.

SEE ALSO
      jq(1)

{GlobalConstants.VERSION}                              May 2024                       PASSENGER(1)"
      );
      Environment.Exit(0);
    }

    /// <summary>
    /// Show help
    /// </summary>
    /// <remarks>
    /// Show help message and exit.
    /// </remarks>
    public static void Help()
    {
      Console.Write(@$"Passenger CLI {GlobalConstants.VERSION}
  Copyright (C) 2024 Elagoht
  
  Store, retrieve, manage and generate passphrases securely using your own encode/decode algorithm. Every passenger client is created by user's itself and unique.

Usage:
  passenger [command] [*args]

Commands:
  login      -l [username] [passphrase] : generate a JWT token to use other commands
  register   -r [username] [passphrase] : register a passphrase to the passenger client
  reset      -R [jwt] [new]             : reset the passphrase of the passenger client
  fetchAll   -a [jwt]                   : list all entries without their passphrases
  query      -q [jwt] [keyword]         : list search results without their passphrases
  fetch      -f [jwt] [uuid]            : retrieve an entry by its uuid with its passphrase
  create     -c [jwt] [json]            : store an entry with the given json
  update     -u [jwt] [uuid] [json]     : update an entry by its uuid
  delete     -d [jwt] [uuid]            : delete an entry by its index
  statis     -s [jwt]                   : show statistics of the database
  declare    -D [jwt] [key] [value]     : declare a new key-value pair
  forget     -F [jwt] [key]             : forget a key-value pair
  constants  -C [jwt]                   : list all declared constants
  generate   -g [length]                : generate a passphrase with the given length
  manipulate -m [passphrase]            : manipulate a passphrase
  version    -v --version               : show the version and exit
  help       -h --help                  : show this help message and exit
  man        -M                         : show the manual page, if available
");
      Environment.Exit(0);
    }

    // *** Version *** //
    public static void Version()
    {
      Console.WriteLine($"Passenger CLI {GlobalConstants.VERSION}");
      Environment.Exit(0);
    }
  }
}