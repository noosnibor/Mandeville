using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for the saved wording of editable messages (one row per message).</summary>
public interface IMessageTemplateRepository
{
    Task<List<MessageTemplateModel>> SelectMessageTemplate(string? code = null, CancellationToken cancellationToken = default);

    /// <summary>Saves the given messages, adding any that were never saved before.</summary>
    Task<ToastMessageModel> SaveMessageTemplates(IEnumerable<MessageTemplateModel> templates, string? who, CancellationToken cancellationToken = default);
}

#endregion

public class MessageTemplateRepository(IDataAccess dataAccess) : IMessageTemplateRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<MessageTemplateModel>> SelectMessageTemplate(string? code = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fstrCode", code);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<MessageTemplateModel>("mandeville.sp_SelectMessageTemplate", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations

    /// <inheritdoc />
    public async Task<ToastMessageModel> SaveMessageTemplates(IEnumerable<MessageTemplateModel> templates, string? who, CancellationToken cancellationToken = default)
    {
        if (templates is null)
            return Invalid();

        try
        {
            foreach (var template in templates)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@fstrCode", template.fstrCode);
                parameters.Add("@fstrText", template.fstrText);
                parameters.Add("@fstrWho", who);
                parameters.Add("@fdtmWhen", AppClock.Now);

                await WithCancellation(
                    __dataAccess.ExecuteAsync("mandeville.sp_SaveMessageTemplate", CommandType.StoredProcedure, parameters),
                    cancellationToken);
            }

            return Success("Messages were saved");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    #endregion
}
