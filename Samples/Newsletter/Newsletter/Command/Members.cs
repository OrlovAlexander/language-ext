using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using Newsletter.Effects;
using Newsletter.Data;

namespace Newsletter.Command;

/// <summary>
/// Member access
/// </summary>
public static class Members<M, RT>
    where RT : 
        Has<M, FileIO>,
        Has<M, EncodingIO>,
        Has<M, DirectoryIO>,
        Has<M, Config>
    where M :
        MonadIO<M>,
        Fallible<M>
{
    public static K<M, Seq<Member>> readAll =>
        from folder  in Config<M, RT>.membersFolder
        from path    in readFirstFile(folder)
        select readMembers(path).Filter(m => m.SubscribedToEmails);

    static K<M, string> readFirstFile(string folder) =>
        Directory<M, RT>.enumerateFiles(folder, "*.csv")
                        .Map(fs => fs.OrderDescending()
                                     .AsIterable()
                                     .Head())
                        .Bind(path => path.Match(
                                  Some: pure<M, string>,
                                  None: error<M, string>(Error.New($"no member files found in {folder}"))));

    static Seq<Member> readMembers(string path)
    {
        var       config  = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
        using var reader  = new StreamReader(path);
        using var csv     = new CsvReader(reader, config);
        return csv.GetRecords<Row>()
                  .AsIterable()
                  .Map(r => new Member(r.id, r.email, r.name, r.subscribed_to_emails == "true", r.tiers == "Supporter"))
                  .ToSeq()
                  .Strict();
    }

    record Row(
        string id,
        string email,
        string name,
        string note,
        string subscribed_to_emails,
        string complimentary_plan,
        string stripe_customer_id,
        string created_at,
        string deleted_at,
        string labels,
        string tiers);
}


