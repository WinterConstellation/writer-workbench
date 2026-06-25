using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WriterWorkbench.Core.Story;

namespace WriterWorkbench;

public partial class WorkbenchRelationshipMapView : System.Windows.Controls.UserControl
{
    private StoryStructureStore? _store;
    private string? _draggedEntityId;
    private System.Windows.Point _dragOffset;

    public WorkbenchRelationshipMapView()
    {
        InitializeComponent();
    }

    public int EntityCount => DetachedRelationshipEntityList.Items.Count;

    public int RelationshipCount => DetachedRelationshipList.Items.Count;

    public int CanvasElementCount => DetachedRelationshipMapCanvas.Children.Count;

    public string SummaryText => DetachedRelationshipMapSummaryText.Text;

    public async Task LoadAsync(StoryStructureStore store, CancellationToken token)
    {
        _store = store;
        var entities = await store.LoadEntitiesAsync(token);
        var relationships = await store.LoadRelationshipsAsync(token);
        var layout = await store.LoadRelationLayoutAsync(token);
        var relationshipItems = relationships
            .Select(relationship => DetachedRelationshipListItem.From(relationship, entities))
            .ToList();
        var entityItems = entities
            .Select(entity => new DetachedRelationshipEntityListItem(entity))
            .ToList();

        await Dispatcher.InvokeAsync(() =>
        {
            DetachedRelationshipEntityList.ItemsSource = entityItems;
            DetachedRelationshipList.ItemsSource = relationshipItems;
            DetachedRelationshipMapSummaryText.Text = entities.Count == 0
                ? "등록된 인물이 없습니다."
                : $"{entities.Count:N0}명 / 관계 {relationships.Count:N0}개 - {string.Join(", ", entities.Take(3).Select(entity => entity.Name))}";
            Render(entities, relationships, layout);
        });
    }

    private void Render(
        IReadOnlyList<StoryEntity> entities,
        IReadOnlyList<StoryRelationship> relationships,
        IReadOnlyList<StoryMapNodeLayout> layout)
    {
        DetachedRelationshipMapCanvas.Children.Clear();
        var positions = CreatePositions(entities, layout);
        foreach (var relationship in relationships)
        {
            if (!positions.TryGetValue(relationship.SourceEntityId, out var source) ||
                !positions.TryGetValue(relationship.TargetEntityId, out var target))
            {
                continue;
            }

            DetachedRelationshipMapCanvas.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = source.X + 65,
                Y1 = source.Y + 26,
                X2 = target.X + 65,
                Y2 = target.Y + 26,
                Stroke = CreateBrush("#64748B"),
                StrokeThickness = relationship.IsDirectional ? 2.5 : 1.5
            });

            var label = new TextBlock
            {
                Text = relationship.Label,
                Background = CreateBrush("#FFFFFF"),
                Foreground = CreateBrush("#111827"),
                Padding = new Thickness(4, 1, 4, 1),
                FontSize = 11
            };
            Canvas.SetLeft(label, (source.X + target.X) / 2 + 52);
            Canvas.SetTop(label, (source.Y + target.Y) / 2 + 16);
            DetachedRelationshipMapCanvas.Children.Add(label);
        }

        foreach (var entity in entities)
        {
            var node = CreateNode(entity);
            Canvas.SetLeft(node, positions[entity.Id].X);
            Canvas.SetTop(node, positions[entity.Id].Y);
            DetachedRelationshipMapCanvas.Children.Add(node);
        }
    }

    private Border CreateNode(StoryEntity entity)
    {
        var stack = new StackPanel { Margin = new Thickness(8, 6, 8, 6) };
        stack.Children.Add(new TextBlock
        {
            Text = entity.Name,
            FontWeight = FontWeights.SemiBold,
            Foreground = CreateBrush("#FFFFFF"),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"{entity.Type} | {entity.Role}",
            Foreground = CreateBrush("#E5E7EB"),
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var node = new Border
        {
            Width = 130,
            Height = 52,
            CornerRadius = new CornerRadius(6),
            Background = CreateBrushOrFallback(entity.Color, "#2563EB"),
            BorderBrush = CreateBrush("#111827"),
            BorderThickness = new Thickness(1),
            Child = stack,
            Tag = entity.Id,
            Cursor = System.Windows.Input.Cursors.SizeAll,
            ToolTip = $"{entity.Name}\n{entity.Summary}"
        };
        node.MouseLeftButtonDown += Node_MouseLeftButtonDown;
        node.MouseMove += Node_MouseMove;
        node.MouseLeftButtonUp += Node_MouseLeftButtonUp;
        return node;
    }

    private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border node || node.Tag is not string entityId)
        {
            return;
        }

        _draggedEntityId = entityId;
        _dragOffset = e.GetPosition(node);
        node.CaptureMouse();
        e.Handled = true;
    }

    private void Node_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_draggedEntityId is null ||
            sender is not Border node ||
            e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var point = e.GetPosition(DetachedRelationshipMapCanvas);
        Canvas.SetLeft(node, Math.Max(0, point.X - _dragOffset.X));
        Canvas.SetTop(node, Math.Max(0, point.Y - _dragOffset.Y));
    }

    private async void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_store is null || _draggedEntityId is null || sender is not Border node)
        {
            return;
        }

        var entityId = _draggedEntityId;
        _draggedEntityId = null;
        node.ReleaseMouseCapture();
        await _store.SaveNodeLayoutAsync(entityId, Canvas.GetLeft(node), Canvas.GetTop(node), CancellationToken.None);
        await LoadAsync(_store, CancellationToken.None);
    }

    private static Dictionary<string, System.Windows.Point> CreatePositions(
        IReadOnlyList<StoryEntity> entities,
        IReadOnlyList<StoryMapNodeLayout> layout)
    {
        var saved = layout.ToDictionary(node => node.EntityId, node => new System.Windows.Point(node.X, node.Y), StringComparer.OrdinalIgnoreCase);
        var positions = new Dictionary<string, System.Windows.Point>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < entities.Count; index++)
        {
            var entity = entities[index];
            positions[entity.Id] = saved.TryGetValue(entity.Id, out var point)
                ? point
                : new System.Windows.Point(44 + (index % 4) * 176, 44 + (index / 4) * 118);
        }

        return positions;
    }

    private static System.Windows.Media.Brush CreateBrush(string color)
    {
        return (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(color)!;
    }

    private static System.Windows.Media.Brush CreateBrushOrFallback(string color, string fallback)
    {
        try
        {
            return CreateBrush(color);
        }
        catch (FormatException)
        {
            return CreateBrush(fallback);
        }
    }

    private sealed record DetachedRelationshipEntityListItem(StoryEntity Entity)
    {
        public string Display => $"{Entity.Name} | {Entity.Type} | {Entity.Role}";
    }

    private sealed record DetachedRelationshipListItem(
        StoryRelationship Relationship,
        string SourceName,
        string TargetName)
    {
        public string Display => $"{SourceName} -> {TargetName} | {Relationship.Label}";

        public static DetachedRelationshipListItem From(
            StoryRelationship relationship,
            IEnumerable<StoryEntity> entities)
        {
            var names = entities.ToDictionary(entity => entity.Id, entity => entity.Name, StringComparer.OrdinalIgnoreCase);
            return new DetachedRelationshipListItem(
                relationship,
                names.GetValueOrDefault(relationship.SourceEntityId, relationship.SourceEntityId),
                names.GetValueOrDefault(relationship.TargetEntityId, relationship.TargetEntityId));
        }
    }
}
