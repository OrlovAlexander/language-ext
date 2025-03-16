using Newsletter.UI;
using Newsletter.Data;
using Newsletter.Effects;
using Newsletter.Effects.Traits;
using SendGrid.Helpers.Mail;

namespace Newsletter.Command;

public static class Email<M, RT>
    where RT : 
        Has<M, EmailIO>,
        Has<M, ConsoleIO>,
        Has<M, Config>
    where M :
        MonadIO<M>,
        Fallible<M>
{
    public static K<M, Unit> sendToAll(Seq<Member> members, Letter letter) =>
        members.Traverse(m => send(m.Name, m.Email, letter))
               .IgnoreF();

    public static K<M, Unit> send(string name, string email, Letter letter) =>
        Effects.Email<M, RT>
               .send(new EmailAddress("noreply@paullouth.com", "Notes from a Small Functional Island"),
                     new EmailAddress(email, name),
                     letter.Title,
                     letter.PlainText,
                     letter.Html)
              | @catch(Display<M, RT>.error); 
}
