using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PremsCart.Api.Data;
using PremsCart.Api.Models;

namespace PremsCart.Api.SignalR;

[Authorize]
public sealed class ChatHub(PremsCartDbContext db, ChatPresence presence) : Hub
{
    private int CurrentUserId => int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static string Group(int id) => $"conversation:{id}";

    public override async Task OnConnectedAsync()
    {
        if (presence.Connect(CurrentUserId, Context.ConnectionId))
            await NotifyPresence(true);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (presence.Disconnect(CurrentUserId, Context.ConnectionId))
            await NotifyPresence(false);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task NotifyPresence(bool online)
    {
        var me = CurrentUserId;
        var contacts = await db.Conversations.AsNoTracking()
            .Where(x => x.BuyerId == me || x.SellerId == me)
            .Select(x => x.BuyerId == me ? x.SellerId : x.BuyerId).Distinct().ToListAsync();
        await Clients.Users(contacts.Select(id => id.ToString()))
            .SendAsync("PresenceChanged", new { userId = me, online });
    }

    public async Task<bool> JoinConversation(int conversationId)
    {
        var me = CurrentUserId;
        if (!await db.Users.AnyAsync(x => x.Id == me && x.IsVerified && x.Status == "Active" && x.TokenVersion.ToString() == Context.User!.FindFirstValue("version")))
            throw new HubException("Account is no longer verified.");
        var conversation = await db.Conversations.AsNoTracking()
            .Where(x => x.Id == conversationId && (x.BuyerId == me || x.SellerId == me))
            .Select(x => new { x.BuyerId, x.SellerId }).SingleOrDefaultAsync();
        if (conversation is null) throw new HubException("Conversation not found.");
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(conversationId));
        return presence.IsOnline(conversation.BuyerId == me ? conversation.SellerId : conversation.BuyerId);
    }

    public Task LeaveConversation(int conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(conversationId));

    public async Task SendMessage(int conversationId, string text)
    {
        var me = CurrentUserId;
        var conversation = await db.Conversations.AsNoTracking()
            .Where(x => x.Id == conversationId && (x.BuyerId == me || x.SellerId == me))
            .Select(x => new { x.BuyerId, x.SellerId }).SingleOrDefaultAsync();
        if (conversation is null) throw new HubException("Conversation not found.");
        if (!await db.Users.AnyAsync(x => x.Id == me && x.IsVerified && x.Status == "Active" && x.TokenVersion.ToString() == Context.User!.FindFirstValue("version")))
            throw new HubException("Account is no longer verified.");
        text = text?.Trim() ?? "";
        if (text.Length is < 1 or > 2000) throw new HubException("Message must be 1–2000 characters.");
        var message = new Message
        {
            ConversationId = conversationId, SenderId = me,
            MessageText = text, SentAt = DateTime.UtcNow
        };
        db.Messages.Add(message);
        await db.SaveChangesAsync();
        var result = new
        {
            message.Id, message.ConversationId, message.SenderId,
            message.MessageText, message.IsRead, message.SentAt
        };
        // Also deliver to the sender's other tabs and recipient's inbox.
        await Clients.Group(Group(conversationId)).SendAsync("ReceiveMessage", result);
        await Clients.Users(new[] { conversation.BuyerId.ToString(), conversation.SellerId.ToString() })
            .SendAsync("InboxChanged", conversationId);
    }
}
