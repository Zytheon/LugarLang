//using Android.App.Admin;
using LugarLang.Mobile.Services.Developer;
using Microsoft.Maui.Controls;

namespace LugarLang.Mobile.Controls.Developer;

public partial class DeveloperOverlay : ContentView
{

    private Element? editableRoot;

    private View? selectedElement;
    private readonly List<View>
    overlapCandidates = new();

    private View? overlapCandidateSelection;

    private double inspectorStartX;

    private double inspectorStartY;

    private double verticalOffset;

    private double overlapChooserStartX;
    private double overlapChooserStartY;
    private Rect cachedOverlayAbsoluteBounds;


    private readonly Stack<UIEditOperation>
    undoStack = new();
    private readonly DeveloperCoordinateMapper
    coordinateMapper =
        new();

    private readonly
    DeveloperEditableElementResolver
    editableElementResolver =
        new();

    private readonly Stack<UIEditOperation>
        redoStack = new();

    private readonly Dictionary<
        View,
        (double TranslationX,
         double TranslationY)>
        sessionInitialPositions = new();

    private bool multiSelectMode;

    private readonly List<View>
        multiSelectedElements = new();

    public DeveloperOverlay()
    {
        InitializeComponent();

        IsVisible = false;
        System.Diagnostics.Debug.WriteLine(
    "DeveloperOverlay is called");

        AbsoluteLayout? editorSurface =
            this.FindByName<AbsoluteLayout>(
                "EditorSurface");

        AbsoluteLayout? candidateSurface =
            this.FindByName<AbsoluteLayout>(
                "CandidateSurface");

        if (editorSurface != null)
        {
            editorSurface.IsVisible =
                false;

            editorSurface.InputTransparent =
                true;
            System.Diagnostics.Debug.WriteLine(
    "DeveloperOverlay:editorSurface != null, editorSurface.InputTransparent = true");
        }

        if (candidateSurface != null)
        {
            candidateSurface.IsVisible =
                false;

            candidateSurface.InputTransparent =
                true;
        }
    }



    public void SetDeveloperMode(bool enabled)
    {
        if (enabled)
        {
            if (editableRoot != null)
            {
                BeginEditMode();
            }
            return;
        }

        StopEditMode();
        TeardownEditor();
        IsVisible = false;
    }

    public Func<Task>? CommitRequested
    {
        get;
        set;
    }

    public Action<
    VisualElement,
    double,
    double>?
    LayoutChanged
    {
        get;
        set;
    }

    public Action? ChangesDiscarded
    {
        get;
        set;
    }

    public Action? EditingCompleted
    {
        get;
        set;
    }

    private async void OnCommitClicked(
        object sender,
        EventArgs e)
    {
        bool confirmed =
            await Shell.Current.DisplayAlertAsync(
                "Commit Changes",
                "This will make your current developer changes permanent. " +
                "They will remain after you close and reopen the app.\n\n" +
                "Do you want to commit these changes?",
                "Commit",
                "Cancel");

        if (!confirmed)
        {
            return;
        }

        if (CommitRequested != null)
        {
            await CommitRequested();
        }

        await Shell.Current.DisplayAlertAsync(
            "Changes Committed",
            "Your developer changes are now permanent.",
            "OK");
    }

    private sealed class UIEditOperation
    {
        public View Element { get; }

        public double OldTranslationX { get; }
        public double OldTranslationY { get; }
        public double NewTranslationX { get; }
        public double NewTranslationY { get; }

        public double OldWidth { get; }
        public double OldHeight { get; }
        public double NewWidth { get; }
        public double NewHeight { get; }

        public UIEditOperation(
            View element,
            double oldTranslationX,
            double oldTranslationY,
            double newTranslationX,
            double newTranslationY,
            double oldWidth,
            double oldHeight,
            double newWidth,
            double newHeight)
        {
            Element = element;

            OldTranslationX = oldTranslationX;
            OldTranslationY = oldTranslationY;
            NewTranslationX = newTranslationX;
            NewTranslationY = newTranslationY;

            OldWidth = oldWidth;
            OldHeight = oldHeight;
            NewWidth = newWidth;
            NewHeight = newHeight;
        }
    }

    public void SetEditableRoot(
        Element root)
    {
        editableRoot =
            root;
    }

    public void SetVerticalOffset(
    double offset)
    {
        verticalOffset =
            offset;
    }


    public void BeginEditMode()
    {
        StartEditMode();
    }

    public void BeginEditMode(
        bool startInMultiSelectMode)
    {
        multiSelectMode =
            startInMultiSelectMode;

        StartEditMode();
    }

    private void StartEditMode()
    {

        System.Diagnostics.Debug.WriteLine(
    "OVERLAY: StartEditMode called");



        IsVisible =
            true;

        AbsoluteLayout? editorSurface =
            this.FindByName<AbsoluteLayout>(
                "EditorSurface");

        if (editorSurface == null)
        {
            return;
        }

        CloseOverlapChooser();

        undoStack.Clear();

        multiSelectedElements.Clear();

        Border? multiSelectToolbar =
            this.FindByName<Border>(
                "MultiSelectToolbar");

        if (multiSelectToolbar != null)
        {
            multiSelectToolbar.IsVisible =
                multiSelectMode;
        }

        redoStack.Clear();
        sessionInitialPositions.Clear();

        selectedElement = null;

        editorSurface.IsVisible =
            true;

        editorSurface.InputTransparent =
            false;

        HideSelectionSurface();
        HideInspectorPanelOnly();

        UpdateHistoryButtons();
    }

    private void HideSelectionSurface()
    {
        Border? selectionSurface =
            this.FindByName<Border>(
                "SelectionSurface");

        if (selectionSurface != null)
        {
            selectionSurface.IsVisible =
                false;
        }
    }

    private void HideInspectorPanelOnly()
    {
        Border? inspector =
            this.FindByName<Border>(
                "InspectorPanel");

        if (inspector != null)
        {
            inspector.IsVisible =
                false;
        }
    }

    private void OnEditorSurfaceTapped(
        object sender,
        TappedEventArgs e)
    {
        if (editableRoot == null)
        {
            return;
        }

        Point? position =
            e.GetPosition(
                editableRoot as VisualElement);

        if (position == null)
        {
            return;
        }

        List<View> elements =
            FindElementsAtPoint(
                editableRoot,
                position.Value);

        List<View> editableElements =
            ResolveEditableCandidates(
                elements);

        if (editableElements.Count == 0)
        {
            if (!multiSelectMode)
            {
                selectedElement = null;

                HideSelectionSurface();
                HideInspectorPanelOnly();
            }

            return;
        }

        if (multiSelectMode)
        {
            if (editableElements.Count == 1)
            {
                ToggleMultiSelect(
                    editableElements[0]);

                return;
            }

            ShowOverlapChooser(
                editableElements,
                position.Value);

            return;
        }

        if (editableElements.Count == 1)
        {
            SelectElement(
                editableElements[0]);

            return;
        }

        ShowOverlapChooser(
            editableElements,
            position.Value);
    }

    private void ToggleMultiSelect(
    View element)
    {
        if (multiSelectedElements.Contains(element))
        {
            multiSelectedElements.Remove(element);
        }
        else
        {
            multiSelectedElements.Add(element);
        }

        ShowMultiSelectPreviews();

        UpdateMultiSelectToolbarLabel();
    }

    private void ShowMultiSelectPreviews()
    {
        AbsoluteLayout? surface =
            this.FindByName<AbsoluteLayout>(
                "CandidateSurface");

        if (surface == null)
        {
            return;
        }

        surface.Children.Clear();

        foreach (
            View element
            in multiSelectedElements)
        {
            Rect mapped =
                coordinateMapper.MapElementToOverlaySpace(
                    element,
                    this);

            mapped = new Rect(
                mapped.X,
                mapped.Y + verticalOffset,
                mapped.Width,
                mapped.Height);

            Border preview =
                new Border
                {
                    BackgroundColor =
                        Colors.Transparent,

                    Stroke =
                        Colors.Red,

                    StrokeThickness =
                        3,

                    InputTransparent =
                        true
                };

            AbsoluteLayout.SetLayoutBounds(
                preview,
                new Rect(
                    mapped.X +
                        element.TranslationX,

                    mapped.Y +
                        element.TranslationY,

                    mapped.Width,
                    mapped.Height));

            surface.Children.Add(
                preview);
        }

        surface.IsVisible =
            multiSelectedElements.Count > 0;
    }

    private void HideMultiSelectPreviews()
    {
        AbsoluteLayout? surface =
            this.FindByName<AbsoluteLayout>(
                "CandidateSurface");

        if (surface != null)
        {
            surface.Children.Clear();

            surface.IsVisible =
                false;
        }
    }

    private void UpdateMultiSelectToolbarLabel()
    {
        Button? groupButton =
            this.FindByName<Button>(
                "MultiSelectGroupButton");

        if (groupButton != null)
        {
            groupButton.Text =
                $"Group Selected ({multiSelectedElements.Count})";

            groupButton.IsEnabled =
                multiSelectedElements.Count > 0;
        }
    }

    private void EndMultiSelectSession(
        bool discarded)
    {
        multiSelectedElements.Clear();

        HideMultiSelectPreviews();

        Border? toolbar =
            this.FindByName<Border>(
                "MultiSelectToolbar");

        if (toolbar != null)
        {
            toolbar.IsVisible =
                false;
        }

        AbsoluteLayout? editorSurface =
            this.FindByName<AbsoluteLayout>(
                "EditorSurface");

        if (editorSurface != null)
        {
            editorSurface.IsVisible =
                false;

            editorSurface.InputTransparent =
                true;
        }

        IsVisible =
            false;

        if (discarded)
        {
            ChangesDiscarded?.Invoke();
        }
        else
        {
            EditingCompleted?.Invoke();
        }
    }

    private void OnMultiSelectGroupClicked(
    object sender,
    EventArgs e)
    {
        if (multiSelectedElements.Count == 0)
        {
            return;
        }

        ShowGroupPanel();
    }

    private void OnMultiSelectCancelClicked(
        object sender,
        EventArgs e)
    {
        EndMultiSelectSession(
            discarded: true);
    }

    private List<View> FindElementsAtPoint(
        Element element,
        Point point)
    {
        List<View> candidates =
            new();

        CollectSelectableViews(
            element,
            candidates);

        return candidates
            .Where(
                view =>
                    IsPointInsideElement(
                        view,
                        point))
            .ToList();
    }

    private List<View> ResolveEditableCandidates(
    List<View> elements)
    {
        List<View> resolved =
            new();

        foreach (View element in elements)
        {
            View editable =
                editableElementResolver.Resolve(
                    element);

            bool alreadyExists =
                resolved.Any(
                    existing =>
                        ReferenceEquals(
                            existing,
                            editable));

            if (!alreadyExists)
            {
                resolved.Add(
                    editable);
            }
        }

        return resolved;
    }

    private void ShowOverlapChooser(
    List<View> elements,
    Point point)
    {

        System.Diagnostics.Debug.WriteLine(
    "OVERLAY: ShowOverlapCandidatePreviews called");

        overlapCandidates.Clear();

        overlapCandidates.AddRange(
            elements);

        overlapCandidateSelection =
            elements.LastOrDefault();

        VerticalStackLayout? options =
            this.FindByName<VerticalStackLayout>(
                "OverlapOptionsLayout");

        Border? chooser =
            this.FindByName<Border>(
                "OverlapChooser");

        Button? confirm =
            this.FindByName<Button>(
                "OverlapConfirmButton");

        if (options == null ||
            chooser == null ||
            confirm == null)
        {
            return;
        }

        options.Children.Clear();

        foreach (
            View element
            in overlapCandidates)
        {
            Button optionButton =
                CreateOverlapOptionButton(
                    element);

            options.Children.Add(
                optionButton);
        }

        AbsoluteLayout? editorSurface =
    this.FindByName<AbsoluteLayout>(
        "EditorSurface");

        if (editorSurface != null)
        {
            editorSurface.InputTransparent =
                true;
            System.Diagnostics.Debug.WriteLine(
    "ShowOverlapChooser:editorSurface != null, editorSurface.InputTransparent = true");
        }

        chooser.IsVisible =
            true;

        confirm.IsEnabled =
            overlapCandidateSelection != null;

        ShowOverlapCandidatePreviews();
    }

    private Button CreateOverlapOptionButton(
        View element)
    {
        Button button =
            new Button
            {
                HorizontalOptions =
                    LayoutOptions.Fill,

                BorderWidth = 0
            };

        button.Text =
            GetElementDescription(
                element);

        button.Clicked +=
            (sender, args) =>
            {
                if (multiSelectMode)
                {
                    if (multiSelectedElements.Contains(element))
                    {
                        multiSelectedElements.Remove(element);
                    }
                    else
                    {
                        multiSelectedElements.Add(element);
                    }
                }
                else
                {
                    overlapCandidateSelection =
                        element;
                }

                UpdateOverlapOptionButtons();

                ShowOverlapCandidatePreviews();
            };

        return button;
    }

    private string GetElementDescription(
        View element)
    {
        string? group =
            DeveloperEditable.GetEditableGroup(
                element);

        string description;

        if (!string.IsNullOrWhiteSpace(group))
        {
            description =
                $"{group}  ({element.GetType().Name})";
        }
        else
        {
            string name =
                string.IsNullOrWhiteSpace(
                    element.AutomationId)
                    ? string.Empty
                    : element.AutomationId;

            string text =
                GetElementText(
                    element);

            string type =
                element.GetType().Name;

            if (!string.IsNullOrWhiteSpace(name))
            {
                description =
                    $"{name}  ({type})";
            }
            else if (!string.IsNullOrWhiteSpace(text))
            {
                description =
                    $"{text}  ({type})";
            }
            else
            {
                description =
                    type;
            }
        }

        string? groupName =
            DeveloperGroup.GetGroupName(
                element);

        if (!string.IsNullOrWhiteSpace(groupName))
        {
            description +=
                $"  [Group: {groupName}]";
        }

        return description;
    }

    private void UpdateOverlapOptionButtons()
    {
        VerticalStackLayout? options =
            this.FindByName<VerticalStackLayout>(
                "OverlapOptionsLayout");

        if (options == null)
        {
            return;
        }

        for (
            int i = 0;
            i < overlapCandidates.Count;
            i++)
        {
            if (options.Children[i]
                is not Button button)
            {
                continue;
            }

            bool isSelected =
                multiSelectMode
                    ? multiSelectedElements.Contains(overlapCandidates[i])
                    : ReferenceEquals(
                        overlapCandidates[i],
                        overlapCandidateSelection);

            button.BorderWidth =
                isSelected
                    ? 2
                    : 0;

            button.BorderColor =
                isSelected
                    ? Colors.Red
                    : Colors.Transparent;
        }

        Button? confirm =
            this.FindByName<Button>(
                "OverlapConfirmButton");

        if (confirm != null)
        {
            confirm.IsEnabled =
                multiSelectMode
                    ? multiSelectedElements.Count > 0
                    : overlapCandidateSelection != null;
        }
    }
    private void ShowOverlapCandidatePreviews()
    {
        AbsoluteLayout? surface =
            this.FindByName<AbsoluteLayout>(
                "CandidateSurface");

        if (surface == null)
        {
            return;
        }

        surface.Children.Clear();

        foreach (
            View element
            in overlapCandidates)
        {
            Rect mapped =
                coordinateMapper.MapElementToOverlaySpace(
                    element,
                    cachedOverlayAbsoluteBounds);

            mapped = new Rect(
                mapped.X,
                mapped.Y + verticalOffset,
                mapped.Width,
                mapped.Height);

            Border preview =
                new Border
                {
                    BackgroundColor =
                        Colors.Transparent,

                    Stroke =
    (multiSelectMode
        ? multiSelectedElements.Contains(element)
        : ReferenceEquals(element, overlapCandidateSelection))
        ? Colors.Red
        : Colors.Gray,

                    StrokeThickness =
    (multiSelectMode
        ? multiSelectedElements.Contains(element)
        : ReferenceEquals(element, overlapCandidateSelection))
        ? 3
        : 1,

                    InputTransparent =
                        true
                };

            AbsoluteLayout.SetLayoutBounds(
                preview,
                new Rect(
                    mapped.X +
                        element.TranslationX,

                    mapped.Y +
                        element.TranslationY,

                    mapped.Width,
                    mapped.Height));

            surface.Children.Add(
                preview);
        }

        surface.IsVisible =
            true;
    }

    private void OnOverlapConfirmClicked(
    object sender,
    EventArgs e)
    {
        if (multiSelectMode)
        {
            CloseOverlapChooser();

            ShowMultiSelectPreviews();

            UpdateMultiSelectToolbarLabel();

            return;
        }

        if (overlapCandidateSelection == null)
        {
            return;
        }

        View selected =
            overlapCandidateSelection;

        CloseOverlapChooser();

        SelectElement(
            selected);
    }

    private void ShowGroupPanel()
    {
        Border? groupPanel =
            this.FindByName<Border>(
                "GroupPanel");

        VerticalStackLayout? membersLayout =
            this.FindByName<VerticalStackLayout>(
                "GroupMembersLayout");

        Entry? nameEntry =
            this.FindByName<Entry>(
                "GroupNameEntry");

        if (groupPanel == null ||
            membersLayout == null ||
            nameEntry == null)
        {
            return;
        }

        nameEntry.Text =
            string.Empty;

        membersLayout.Children.Clear();

        foreach (
            View element
            in multiSelectedElements)
        {
            string description =
                GetElementDescription(
                    element);

            string? existingGroupName =
                DeveloperGroup.GetGroupName(
                    element);

            Label memberLabel =
                new Label
                {
                    Text =
                        string.IsNullOrWhiteSpace(existingGroupName)
                            ? description
                            : $"{description}  \u2192 will leave '{existingGroupName}'",

                    FontSize = 13
                };

            membersLayout.Children.Add(
                memberLabel);
        }

        groupPanel.IsVisible =
            true;
    }

    private void OnGroupConfirmClicked(
object sender,
EventArgs e)
    {
        Entry? nameEntry =
            this.FindByName<Entry>(
                "GroupNameEntry");

        string groupName =
            nameEntry?.Text?.Trim() ??
            string.Empty;

        if (string.IsNullOrWhiteSpace(groupName))
        {
            groupName =
                "Unnamed Group";
        }

        string newGroupId =
            Guid.NewGuid().ToString();

        foreach (
            View element
            in multiSelectedElements)
        {
            DeveloperGroup.SetGroupId(
                element,
                newGroupId);

            DeveloperGroup.SetGroupName(
                element,
                groupName);
        }

        CloseGroupPanel();

        EndMultiSelectSession(
            discarded: false);
    }

    private void OnGroupCancelClicked(
    object sender,
    EventArgs e)
    {
        multiSelectedElements.Clear();

        CloseGroupPanel();
    }

    private void CloseGroupPanel()
    {
        Border? groupPanel =
            this.FindByName<Border>(
                "GroupPanel");

        if (groupPanel != null)
        {
            groupPanel.IsVisible =
                false;
        }
    }

    private void OnOverlapCancelClicked(
    object sender,
    EventArgs e)
    {
        CloseOverlapChooser();
    }

    private void CloseOverlapChooser()
    {
        Border? chooser =
            this.FindByName<Border>(
                "OverlapChooser");

        AbsoluteLayout? surface =
            this.FindByName<AbsoluteLayout>(
                "CandidateSurface");

        AbsoluteLayout? editorSurface =
            this.FindByName<AbsoluteLayout>(
                "EditorSurface");

        if (chooser != null)
        {
            chooser.IsVisible =
                false;
        }

        if (surface != null)
        {
            surface.Children.Clear();

            surface.IsVisible =
                false;
        }

        if (editorSurface != null)
        {
            editorSurface.InputTransparent =
                false;
        }

        overlapCandidates.Clear();

        overlapCandidateSelection =
            null;
    }

    private void CollectSelectableViews(
    Element element,
    List<View> candidates)
    {
        if (element is View view &&
            ShouldBeSelectable(view))
        {
            candidates.Add(
                view);
        }

        if (element is IVisualTreeElement visualElement)
        {
            foreach (
                IVisualTreeElement child
                in visualElement
                    .GetVisualChildren())
            {
                if (child is Element childElement)
                {
                    CollectSelectableViews(
                        childElement,
                        candidates);
                }
            }
        }
    }

    private bool IsPointInsideElement(
        View element,
        Point point)
    {
        Rect bounds =
            coordinateMapper.GetBoundsRelativeTo(
                element,
                editableRoot);

        bounds.X +=
            element.TranslationX;

        bounds.Y +=
            element.TranslationY;

        return bounds.Contains(
            point);
    }

    private void StopEditMode()
    {
        AbsoluteLayout? editorSurface =
            this.FindByName<AbsoluteLayout>(
                "EditorSurface");

        if (editorSurface != null)
        {
            editorSurface.InputTransparent =
                true;
            System.Diagnostics.Debug.WriteLine(
    "StopEditMode:editorSurface != null, editorSurface.InputTransparent = true");
        }
    }



    private bool ShouldBeSelectable(
    View element)
    {
        return
            element is Button ||
            element is Label ||
            element is Entry ||
            element is Editor ||
            element is Image ||
            element is Switch ||
            element is Picker ||
            element is CollectionView ||
            element is ScrollView ||
            element is Border;
    }

    private void SelectElement(
        View element)
    {
        if (!sessionInitialPositions.ContainsKey(
                element))
        {
            sessionInitialPositions[element] =
                (
                    element.TranslationX,
                    element.TranslationY);
        }

        selectedElement =
            element;


        ShowSelectionSurface();

        Border? inspector =
            this.FindByName<Border>(
                "InspectorPanel");

        if (inspector != null)
        {
            inspector.IsVisible =
                true;
        }

        UpdateInspector(
            element);

        UpdateHistoryButtons();
    }

    private void UpdateInspector(
        View element)
    {
        Label? typeLabel =
            this.FindByName<Label>(
                "InspectorTypeLabel");

        Label? nameLabel =
            this.FindByName<Label>(
                "InspectorNameLabel");

        Label? textLabel =
            this.FindByName<Label>(
                "InspectorTextLabel");

        Label? positionLabel =
            this.FindByName<Label>(
                "InspectorPositionLabel");

        Entry? widthEntry =
            this.FindByName<Entry>(
                "WidthEntry");

        Entry? heightEntry =
            this.FindByName<Entry>(
                "HeightEntry");

        Label? groupLabel =
    this.FindByName<Label>(
        "InspectorGroupLabel");

        Button? ungroupButton =
            this.FindByName<Button>(
                "UngroupButton");

        string? groupId =
            DeveloperGroup.GetGroupId(
                element);

        string? groupName =
            DeveloperGroup.GetGroupName(
                element);

        bool isGrouped =
            !string.IsNullOrWhiteSpace(groupId);

        if (groupLabel != null)
        {
            groupLabel.IsVisible =
                isGrouped;

            groupLabel.Text =
                isGrouped
                    ? $"Part of group: {groupName}"
                    : string.Empty;
        }

        if (ungroupButton != null)
        {
            ungroupButton.IsVisible =
                isGrouped;
        }

        if (widthEntry != null)
        {
            widthEntry.Text =
                element.Width.ToString("0");
        }

        if (heightEntry != null)
        {
            heightEntry.Text =
                element.Height.ToString("0");
        }

        if (typeLabel != null)
        {
            typeLabel.Text =
                $"Type: {element.GetType().Name}";
        }

        if (nameLabel != null)
        {
            string name =
                string.IsNullOrWhiteSpace(
                    element.AutomationId)
                    ? "(no AutomationId)"
                    : element.AutomationId;

            nameLabel.Text =
                $"Name: {name}";
        }

        if (textLabel != null)
        {
            string text =
                GetElementText(
                    element);

            textLabel.Text =
                $"Text: {text}";
        }

        if (positionLabel != null)
        {
            positionLabel.Text =
                $"Position: " +
                $"X={element.X:0}, " +
                $"Y={element.Y:0}";
        }

        Border? inspector =
            this.FindByName<Border>(
                "InspectorPanel");

        if (inspector != null)
        {
            inspector.IsVisible =
                true;
        }
    }

    private void OnUngroupClicked(
object sender,
EventArgs e)
    {
        if (selectedElement == null)
        {
            return;
        }

        string? groupId =
            DeveloperGroup.GetGroupId(
                selectedElement);

        if (string.IsNullOrWhiteSpace(groupId))
        {
            return;
        }

        List<View> allSelectableElements =
            new();

        if (editableRoot != null)
        {
            CollectSelectableViews(
                editableRoot,
                allSelectableElements);
        }

        foreach (
            View candidate
            in allSelectableElements)
        {
            if (DeveloperGroup.GetGroupId(candidate) == groupId)
            {
                DeveloperGroup.SetGroupId(
                    candidate,
                    null);

                DeveloperGroup.SetGroupName(
                    candidate,
                    null);
            }
        }

        UpdateInspector(
            selectedElement);
    }

    private void OnSizeEntryCompleted(
        object sender,
        FocusEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(
            "RESIZE: OnSizeEntryCompleted fired");

        if (selectedElement == null)
        {
            System.Diagnostics.Debug.WriteLine(
                "RESIZE: selectedElement is null, aborting");
            return;
        }

        Entry? widthEntry =
            this.FindByName<Entry>(
                "WidthEntry");

        Entry? heightEntry =
            this.FindByName<Entry>(
                "HeightEntry");

        if (widthEntry == null ||
            heightEntry == null)
        {
            System.Diagnostics.Debug.WriteLine(
                "RESIZE: widthEntry or heightEntry is null, aborting");
            return;
        }

        System.Diagnostics.Debug.WriteLine(
            $"RESIZE: widthEntry.Text='{widthEntry.Text}' heightEntry.Text='{heightEntry.Text}'");

        resizeOperationStartWidth =
            selectedElement.Width;

        resizeOperationStartHeight =
            selectedElement.Height;

        if (selectedElement.HorizontalOptions.Alignment == LayoutAlignment.Fill)
        {
            selectedElement.HorizontalOptions = LayoutOptions.Start;
        }

        if (selectedElement.VerticalOptions.Alignment == LayoutAlignment.Fill)
        {
            selectedElement.VerticalOptions = LayoutOptions.Start;
        }

        if (double.TryParse(
                widthEntry.Text,
                out double newWidth))
        {
            selectedElement.WidthRequest =
                newWidth;

            System.Diagnostics.Debug.WriteLine(
                $"RESIZE: set WidthRequest to {newWidth}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine(
                $"RESIZE: FAILED to parse width '{widthEntry.Text}'");
        }

        if (double.TryParse(
                heightEntry.Text,
                out double newHeight))
        {
            selectedElement.HeightRequest =
                newHeight;

            System.Diagnostics.Debug.WriteLine(
                $"RESIZE: set HeightRequest to {newHeight}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine(
                $"RESIZE: FAILED to parse height '{heightEntry.Text}'");
        }

        RecordResizeOperation(
            selectedElement.Width,
            selectedElement.Height);

        System.Diagnostics.Debug.WriteLine(
            $"RESIZE: after set, element.Width={selectedElement.Width} element.Height={selectedElement.Height}");

        RefreshSelectionVisual();
    }

    private void OnSizeEntryFocused(
    object sender,
    FocusEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(
            "RESIZE: Entry focused/clicked into");
    }

    private string GetElementText(
        VisualElement element)
    {
        if (element is Button button)
        {
            return button.Text ??
                   string.Empty;
        }

        if (element is Label label)
        {
            return label.Text ??
                   string.Empty;
        }

        if (element is Entry entry)
        {
            return entry.Text ??
                   string.Empty;
        }

        if (element is Editor editor)
        {
            return editor.Text ??
                   string.Empty;
        }

        return "(none)";
    }

    private void TeardownEditor()
    {
        System.Diagnostics.Debug.WriteLine(
    "Teardown");
        StopEditMode();

        AbsoluteLayout? editorSurface =
            this.FindByName<AbsoluteLayout>(
                "EditorSurface");

        if (editorSurface != null)
        {
            editorSurface.IsVisible =
                false;
        }

        Border? inspector =
            this.FindByName<Border>(
                "InspectorPanel");

        if (inspector != null)
        {
            inspector.IsVisible =
                false;
        }

        selectedElement =
            null;
    }

    private void OnCloseInspectorClicked(
        object sender,
        EventArgs e)
    {

        OnCancelEditClicked(
            sender,
            e);
    }

    private void RefreshSelectionVisual()
    {
        if (selectedElement == null)
        {
            HideSelectionSurface();
            return;
        }

        ShowSelectionSurface();
        UpdateSelectionSurface();
    }

    private void ShowSelectionSurface()
    {
        if (selectedElement == null)
        {
            return;
        }

        Border? selectionSurface =
            this.FindByName<Border>(
                "SelectionSurface");

        AbsoluteLayout? editorSurface =
            this.FindByName<AbsoluteLayout>(
                "EditorSurface");

        if (selectionSurface == null ||
            editorSurface == null)
        {
            return;
        }

        cachedOverlayAbsoluteBounds =
            coordinateMapper.GetAbsoluteBounds(
                this);

        Rect mapped =
            coordinateMapper.MapElementToOverlaySpace(
                selectedElement,
                cachedOverlayAbsoluteBounds);

        mapped = new Rect(
            mapped.X,
            mapped.Y + verticalOffset,
            mapped.Width,
            mapped.Height);

        System.Diagnostics.Debug.WriteLine(
            $"SELECTION DEBUG: " +
            $"ElementAbsolute=({coordinateMapper.GetAbsoluteBounds(selectedElement).X:0.0}, {coordinateMapper.GetAbsoluteBounds(selectedElement).Y:0.0}) " +
            $"OverlayAbsolute=({cachedOverlayAbsoluteBounds.X:0.0}, {cachedOverlayAbsoluteBounds.Y:0.0}) " +
            $"Mapped=({mapped.X:0.0}, {mapped.Y:0.0}) " +
            $"ElementSize=({mapped.Width:0.0}x{mapped.Height:0.0}) " +
            $"EditableRootAbsolute=({(editableRoot is VisualElement erv ? coordinateMapper.GetAbsoluteBounds(erv).X : -1):0.0}, {(editableRoot is VisualElement erv2 ? coordinateMapper.GetAbsoluteBounds(erv2).Y : -1):0.0})");


        AbsoluteLayout? editorSurfaceDebug =
    this.FindByName<AbsoluteLayout>("EditorSurface");

        if (editorSurfaceDebug != null)
        {
            Rect editorSurfaceAbsolute =
                coordinateMapper.GetAbsoluteBounds(editorSurfaceDebug);

            System.Diagnostics.Debug.WriteLine(
                $"EDITORSURFACE DEBUG: Absolute=({editorSurfaceAbsolute.X:0.0}, {editorSurfaceAbsolute.Y:0.0})");
        }

        AbsoluteLayout.SetLayoutBounds(
            selectionSurface,
            new Rect(
                mapped.X +
                    selectedElement.TranslationX,

                mapped.Y +
                    selectedElement.TranslationY,

                mapped.Width,
                mapped.Height));

        selectionSurface.IsVisible =
            true;

        editorSurface.IsVisible =
            true;
    }

    private double moveOperationStartX;

    private double moveOperationStartY;

    private double resizeStartWidth;
    private double resizeStartHeight;

    private double resizeOperationStartWidth;
    private double resizeOperationStartHeight;

    private void OnSelectionPanUpdated(
        object? sender,
        PanUpdatedEventArgs e)
    {
        if (selectedElement == null)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:

                moveOperationStartX =
                    selectedElement.TranslationX;

                moveOperationStartY =
                    selectedElement.TranslationY;

                cachedOverlayAbsoluteBounds =
                coordinateMapper.GetAbsoluteBounds(this);

                break;

            case GestureStatus.Running:

                double newTranslationX =
                    moveOperationStartX +
                    e.TotalX;

                double newTranslationY =
                    moveOperationStartY +
                    e.TotalY;

                double deltaX =
                    newTranslationX -
                    selectedElement.TranslationX;

                double deltaY =
                    newTranslationY -
                    selectedElement.TranslationY;

                selectedElement.TranslationX =
                    newTranslationX;

                selectedElement.TranslationY =
                    newTranslationY;

                ApplyGroupDelta(
                    selectedElement,
                    deltaX,
                    deltaY);

                UpdateInspector(
                    selectedElement);

                UpdateSelectionSurface();

                break;

            case GestureStatus.Completed:

                RecordMoveOperation();

                RefreshSelectionVisual();

                UpdateInspector(
                    selectedElement);

                break;

            case GestureStatus.Canceled:

                selectedElement.TranslationX =
                    moveOperationStartX;

                selectedElement.TranslationY =
                    moveOperationStartY;

                UpdateInspector(
                    selectedElement);

                UpdateSelectionSurface();

                break;
        }
    }

    private void ApplyGroupDelta(
    View movedElement,
    double deltaX,
    double deltaY)
    {
        string? groupId =
            DeveloperGroup.GetGroupId(
                movedElement);

        if (string.IsNullOrWhiteSpace(groupId))
        {
            return;
        }

        List<View> allSelectableElements =
            new();

        if (editableRoot != null)
        {
            CollectSelectableViews(
                editableRoot,
                allSelectableElements);
        }

        foreach (
            View candidate
            in allSelectableElements)
        {
            if (ReferenceEquals(candidate, movedElement))
            {
                continue;
            }

            if (DeveloperGroup.GetGroupId(candidate) == groupId)
            {
                candidate.TranslationX +=
                    deltaX;

                candidate.TranslationY +=
                    deltaY;
            }
        }
    }

    private void RecordMoveOperation()
    {
        if (selectedElement == null)
        {
            return;
        }

        double oldX =
            moveOperationStartX;

        double oldY =
            moveOperationStartY;

        double newX =
            selectedElement.TranslationX;

        double newY =
            selectedElement.TranslationY;

        if (Math.Abs(oldX - newX) < 0.01 &&
            Math.Abs(oldY - newY) < 0.01)
        {
            return;
        }

        undoStack.Push(
            new UIEditOperation(
                selectedElement,
                oldX,
                oldY,
                newX,
                newY,
                selectedElement.Width,
                selectedElement.Height,
                selectedElement.Width,
                selectedElement.Height));

        redoStack.Clear();

        System.Diagnostics.Debug.WriteLine(
    $"DEVELOPER MOVE RECORDED: " +
    $"{selectedElement.GetType().Name} " +
    $"X={newX} Y={newY}");

        LayoutChanged?.Invoke(
    selectedElement,
    newX,
    newY);

        UpdateHistoryButtons();
    }

    private void RecordResizeOperation(
        double newWidth,
        double newHeight)
    {
        if (selectedElement == null)
        {
            return;
        }

        double oldWidth =
            resizeOperationStartWidth;

        double oldHeight =
            resizeOperationStartHeight;

        if (Math.Abs(oldWidth - newWidth) < 0.01 &&
            Math.Abs(oldHeight - newHeight) < 0.01)
        {
            return;
        }

        undoStack.Push(
            new UIEditOperation(
                selectedElement,
                selectedElement.TranslationX,
                selectedElement.TranslationY,
                selectedElement.TranslationX,
                selectedElement.TranslationY,
                oldWidth,
                oldHeight,
                newWidth,
                newHeight));

        redoStack.Clear();

        UpdateHistoryButtons();
    }

    private void OnUndoClicked(
    object sender,
    EventArgs e)
    {
        if (undoStack.Count == 0)
        {
            return;
        }

        UIEditOperation operation =
            undoStack.Pop();

        operation.Element.TranslationX =
            operation.OldTranslationX;

        operation.Element.TranslationY =
            operation.OldTranslationY;

        operation.Element.WidthRequest =
            operation.OldWidth;

        operation.Element.HeightRequest =
            operation.OldHeight;

        redoStack.Push(
            operation);

        selectedElement =
            operation.Element;

        RefreshSelectionVisual();

        UpdateInspector(
            selectedElement);

        UpdateHistoryButtons();

        LayoutChanged?.Invoke(
    selectedElement,
    selectedElement.TranslationX,
    selectedElement.TranslationY);
    }

    private void OnRedoClicked(
    object sender,
    EventArgs e)
    {
        if (redoStack.Count == 0)
        {
            return;
        }

        UIEditOperation operation =
            redoStack.Pop();

        operation.Element.TranslationX =
            operation.NewTranslationX;

        operation.Element.TranslationY =
            operation.NewTranslationY;

        operation.Element.WidthRequest =
            operation.NewWidth;

        operation.Element.HeightRequest =
            operation.NewHeight;

        undoStack.Push(
            operation);

        selectedElement =
            operation.Element;

        RefreshSelectionVisual();

        UpdateInspector(
            selectedElement);

        UpdateHistoryButtons();

        LayoutChanged?.Invoke(
    selectedElement,
    selectedElement.TranslationX,
    selectedElement.TranslationY);
    }

    private void UpdateHistoryButtons()
    {
        Button? undoButton =
            this.FindByName<Button>(
                "UndoButton");

        Button? redoButton =
            this.FindByName<Button>(
                "RedoButton");

        if (undoButton != null)
        {
            undoButton.IsEnabled =
                undoStack.Count > 0;
        }

        if (redoButton != null)
        {
            redoButton.IsEnabled =
                redoStack.Count > 0;
        }
    }
    private void OnSaveEditClicked(
    object sender,
    EventArgs e)
    {
        undoStack.Clear();

        redoStack.Clear();

        sessionInitialPositions.Clear();

        selectedElement =
            null;

        HideSelectionSurface();

        HideInspectorPanelOnly();


        AbsoluteLayout? editorSurface =
            this.FindByName<AbsoluteLayout>(
                "EditorSurface");

        if (editorSurface != null)
        {
            editorSurface.IsVisible =
                false;

            editorSurface.InputTransparent =
                true;
        }

        UpdateHistoryButtons();

        EditingCompleted?.Invoke();

        IsVisible =
            false;
    }

    private void OnCancelEditClicked(
    object sender,
    EventArgs e)
    {
        foreach (
            KeyValuePair<
                View,
                (double TranslationX,
                 double TranslationY)>
            entry
            in sessionInitialPositions)
        {
            entry.Key.TranslationX =
                entry.Value.TranslationX;

            entry.Key.TranslationY =
                entry.Value.TranslationY;
        }

        undoStack.Clear();

        redoStack.Clear();

        sessionInitialPositions.Clear();

        selectedElement =
            null;

        HideSelectionSurface();

        HideInspectorPanelOnly();


        AbsoluteLayout? editorSurface =
            this.FindByName<AbsoluteLayout>(
                "EditorSurface");

        if (editorSurface != null)
        {
            editorSurface.IsVisible =
                false;

            editorSurface.InputTransparent =
                true;
            System.Diagnostics.Debug.WriteLine(
    "OnCancelEditClicked:editorSurface != null, editorSurface.InputTransparent = true");
        }

        UpdateHistoryButtons();

        ChangesDiscarded?.Invoke();


        IsVisible =
            false;
    }


    private void UpdateSelectionSurface()
    {
        if (selectedElement == null)
        {
            return;
        }

        Border? selectionSurface =
            this.FindByName<Border>("SelectionSurface");

        if (selectionSurface == null)
        {
            return;
        }

        Rect mapped =
            coordinateMapper.MapElementToOverlaySpace(
                selectedElement,
                cachedOverlayAbsoluteBounds);

        mapped = new Rect(
            mapped.X,
            mapped.Y + verticalOffset,
            mapped.Width,
            mapped.Height);

        AbsoluteLayout.SetLayoutBounds(selectionSurface, new Rect(
            mapped.X + selectedElement.TranslationX,
            mapped.Y + selectedElement.TranslationY,
            mapped.Width,
            mapped.Height));
    }

    private void OnInspectorPanUpdated(
    object? sender,
    PanUpdatedEventArgs e)
    {
        Border? inspector =
            this.FindByName<Border>(
                "InspectorPanel");

        if (inspector == null)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:

                inspectorStartX =
                    inspector.TranslationX;

                inspectorStartY =
                    inspector.TranslationY;

                break;

            case GestureStatus.Running:

                inspector.TranslationX =
                    inspectorStartX +
                    e.TotalX;

                inspector.TranslationY =
                    inspectorStartY +
                    e.TotalY;

                break;
        }


    }

    private void OnOverlapChooserPanUpdated(
    object? sender,
    PanUpdatedEventArgs e)
    {
        Border? chooser =
            this.FindByName<Border>(
                "OverlapChooser");

        if (chooser == null)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:

                overlapChooserStartX =
                    chooser.TranslationX;

                overlapChooserStartY =
                    chooser.TranslationY;

                break;

            case GestureStatus.Running:

                chooser.TranslationX =
                    overlapChooserStartX +
                    e.TotalX;

                chooser.TranslationY =
                    overlapChooserStartY +
                    e.TotalY;

                break;
        }
    }


}