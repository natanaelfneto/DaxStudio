﻿using System.ComponentModel.Composition;
using Caliburn.Micro;
using DaxStudio.UI.Events;
using DaxStudio.UI.Model;
using DaxStudio.UI.Interfaces;
using System.Windows.Data;
using System;
using System.ComponentModel;
using Serilog;
using System.Windows.Input;
using DaxStudio.Interfaces;
using Dax.ViewModel;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace DaxStudio.UI.ViewModels
{



    public class TableGroupDescription : PropertyGroupDescription, IComparable
    {
        public TableGroupDescription(string propertyName) : base(propertyName) { }

        public override bool NamesMatch(object groupName, object itemName)
        {
            var groupTable = (VpaTableViewModel)groupName;
            var itemTable = (VpaTableViewModel)itemName;
            return base.NamesMatch(groupTable.TableName, itemTable.TableName);
        }
        public override object GroupNameFromItem(object item, int level, CultureInfo culture)
        {
            var col = item as VpaColumnViewModel;
            var rel = item as VpaRelationshipViewModel;
            return col != null?  col.Table:rel.Table ;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }
    }

    public class RelationshipGroupDescription : PropertyGroupDescription
    {
        public RelationshipGroupDescription(string propertyName) : base(propertyName) { }

        public override bool NamesMatch(object groupName, object itemName)
        {
            var groupTable = (VpaTableViewModel)groupName;
            var itemTable = (VpaTableViewModel)itemName;
            return base.NamesMatch(groupTable.TableName, itemTable.TableName);
        }
        public override object GroupNameFromItem(object item, int level, CultureInfo culture)
        {
            var rel = item as VpaRelationshipViewModel;
            return rel.Table;
        }

    }


    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export]
    public class VertiPaqAnalyzerViewModel : ToolWindowBase
        , IHandle<DocumentConnectionUpdateEvent>
        , IHandle<UpdateGlobalOptions>
        , IViewAware
    {

        private readonly IEventAggregator _eventAggregator;
        private readonly DocumentViewModel _currentDocument;
        private readonly IGlobalOptions _globalOptions;

        [ImportingConstructor]
        public VertiPaqAnalyzerViewModel(Dax.ViewModel.VpaModel viewModel, IEventAggregator eventAggregator, DocumentViewModel currentDocument, IGlobalOptions options)
        {
            Log.Debug("{class} {method} {message}", "VertiPaqAnalyzerViewModel", "ctor", "start");
            this.ViewModel = viewModel;
            _globalOptions = options;
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
            _currentDocument = currentDocument;
            Log.Debug("{class} {method} {message}", "VertiPaqAnalyzerViewModel", "ctor", "end");
        }

        private VpaModel _viewModel;
        public Dax.ViewModel.VpaModel ViewModel
        {
            get {return _viewModel; }
            set {
                _viewModel = value;
                _groupedColumns = null;
                _groupedRelationships = null;
                NotifyOfPropertyChange(() => ViewModel);
                NotifyOfPropertyChange(() => GroupedColumns);
                NotifyOfPropertyChange(() => GroupedRelationships);
                NotifyOfPropertyChange(() => TreeviewColumns);
                NotifyOfPropertyChange(() => TreeviewRelationships);
            }
        }

        private ICollectionView _groupedColumns;
        public ICollectionView GroupedColumns
        {
            get
            {
                if (_groupedColumns == null)
                {
                    var cols = ViewModel.Tables.Select(t => new VpaTableViewModel(t, this)).SelectMany(t => t.Columns);
                    _groupedColumns = CollectionViewSource.GetDefaultView(cols);
                    _groupedColumns.GroupDescriptions.Add(new TableGroupDescription("Table"));
                    _groupedColumns.SortDescriptions.Add(new SortDescription("TotalSize", ListSortDirection.Descending));
                }
                return _groupedColumns;
            }
        }

        private ICollectionView _groupedRelationships;
        public ICollectionView GroupedRelationships
        {
            get
            {
                if (_groupedRelationships == null)
                {
                    var rels = ViewModel.TablesWithFromRelationships.Select(t => new VpaTableViewModel(t, this)).SelectMany(t => t.RelationshipsFrom);
                    _groupedRelationships = CollectionViewSource.GetDefaultView(rels);
                    _groupedRelationships.GroupDescriptions.Add(new RelationshipGroupDescription("Table"));
                    _groupedRelationships.SortDescriptions.Add(new SortDescription("UsedSize", ListSortDirection.Descending));
                }
                return _groupedRelationships;
            }
        }

        private ICollectionView _sortedColumns;
        public ICollectionView SortedColumns
        {
            get
            {
                if (_sortedColumns == null)
                {
                    long maxSize = ViewModel.Columns.Max(c => c.TotalSize);
                    var cols = ViewModel.Columns.Select(c => new VpaColumnViewModel(c) { MaxColumnTotalSize = maxSize});
                    
                    _sortedColumns = CollectionViewSource.GetDefaultView(cols);
                    _sortedColumns.SortDescriptions.Add(new SortDescription(nameof(VpaColumn.TotalSize), ListSortDirection.Descending));
                }
                return _sortedColumns;

            }
        }

        public IEnumerable<VpaTable> TreeviewTables { get { return ViewModel.Tables; } }
        public IEnumerable<VpaColumn> TreeviewColumns { get { return ViewModel.Columns; } }
        public IEnumerable<VpaTable> TreeviewRelationships { get { return ViewModel.TablesWithFromRelationships; } }

        // TODO: we might add the database name here
        public override string Title {
            get { return "VertiPaq Analyzer Preview"; }
        }

        public void Handle(DocumentConnectionUpdateEvent message)
        {
            // TODO connect VPA data
            Log.Information("VertiPaq Analyzer Handle DocumentConnectionUpdateEvent call");
        }

        public void MouseDoubleClick(object sender)//, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("clicked!");
        }

        public void Handle(UpdateGlobalOptions message)
        {
            // NotifyOfPropertyChange(() => ShowTraceColumns);
            Log.Information("VertiPaq Analyzer Handle UpdateGlobalOptions call");
        }

        public void SortTableColumn(System.Windows.Controls.DataGridSortingEventArgs e)
        {
            if (SortColumn == e.Column.Header.ToString()) { SortDirection = SortDirection * -1; }
            else { SortDirection = 1; }
            SortColumn = e.Column.Header.ToString();
        }

        public string SortColumn { get; set; }
        public int SortDirection { get; set; }
    }

    public class VpaColumnViewModel 
    {
        readonly VpaColumn _col;

        public VpaColumnViewModel(VpaColumn col)
        {
            _col = col;
        }
        public VpaColumnViewModel(VpaColumn col, VpaTableViewModel table)
        {
            _col = col;
            Table = table;
        }
        public VpaTableViewModel Table { get; }
        public string TableColumnName => _col.TableColumnName;
        public string ColumnName => _col.ColumnName;
        public string TypeName => _col.TypeName;
        public long ColumnCardinality => _col.ColumnCardinality;
        public string Encoding => _col.Encoding;
        public long DataSize => _col.DataSize;
        public long DictionarySize => _col.DictionarySize;
        public long HierarchiesSize => _col.HierarchiesSize;
        public long TotalSize => _col.TotalSize;
        public double PercentageDatabase => _col.PercentageDatabase;
        public double PercentageTable => _col.PercentageTable;
        public long SegmentsNumber => _col.SegmentsNumber;
        public long PartitionsNumber => _col.PartitionsNumber;
        public long ColumnsNumber => 1;
        //public long MaxColumnCardinality { get; set; }
        public long MaxColumnTotalSize { get; set; }
        public double PercentOfMaxTotalSize => Table==null ? 0 : TotalSize / (double)Table.ColumnMaxTotalSize;
        public double PercentOfMaxCardinality =>Table==null ? 0 : ColumnCardinality / (double)Table.ColumnsMaxCardinality;
        public double PercentOfMaxTotalDBSize => MaxColumnTotalSize == 0 ? 0 : TotalSize / (double)MaxColumnTotalSize;
    }

    public class VpaRelationshipViewModel
    {
        VpaRelationship _rel;
        public VpaRelationshipViewModel(VpaRelationship rel, VpaTableViewModel table) {
            Table = table;
            _rel = rel;
        }
        public VpaTableViewModel Table { get; }
        public string RelationshipFromToName => _rel.RelationshipFromToName;
        public long UsedSize => _rel.UsedSize;
        public long FromColumnCardinality => _rel.FromColumnCardinality;
        public long ToColumnCardinality => _rel.ToColumnCardinality;
    }

    public class VpaTableViewModel : IComparable
    {
        private readonly VpaTable _table;
        private readonly VertiPaqAnalyzerViewModel _parentViewModel;
        public VpaTableViewModel(VpaTable table, VertiPaqAnalyzerViewModel parentViewModel)
        {
            _table = table;
            _parentViewModel = parentViewModel;
            Columns = _table.Columns.Select(c => new VpaColumnViewModel(c, this));
            ColumnMaxTotalSize = Columns.Max(c => c.TotalSize);
            ColumnsMaxCardinality = Columns.Max(c => c.ColumnCardinality);
            RelationshipsFrom = _table.RelationshipsFrom.Select(r => new VpaRelationshipViewModel(r, this));
            if (RelationshipsFrom.Count() > 0)
            {
                RelationshipMaxFromCardinality = RelationshipsFrom.Max(r => r.FromColumnCardinality);
                RelationshipMaxToCardinality = RelationshipsFrom.Max(r => r.ToColumnCardinality);
            }
        }

        public string TableName => _table.TableName;
        public long TableSize => _table.TableSize;
        public string ColumnsEncoding => _table.ColumnsEncoding;
        public string ColumnsTypeName => "-";
        public double PercentageDatabase => _table.PercentageDatabase;
        public long ColumnsTotalSize => _table.ColumnsTotalSize;
        public long ColumnsDataSize => _table.ColumnsDataSize;
        public long ColumnsDictionarySize => _table.ColumnsDictionarySize;
        public long ColumnsHierarchySize => _table.ColumnsHierarchiesSize;
        public long RelationshipSize => _table.RelationshipsSize;
        public long UserHierarchiesSize => _table.UserHierarchiesSize;
        public long ColumnsNumber => _table.ColumnsNumber;
        public long RowsCount => _table.RowsCount;
        public long SegmentsNumber => _table.SegmentsNumber;
        public long PartitionsNumber => _table.PartitionsNumber;

        public IEnumerable<VpaColumnViewModel> Columns { get; }
        public IEnumerable<VpaRelationshipViewModel> RelationshipsFrom { get; }
        public long RelationshipMaxFromCardinality { get; }
        public long ColumnMaxTotalSize { get; }
        public long ColumnsMaxCardinality { get; }

        public int CompareTo(object obj)
        {
            var objTable = (VpaTableViewModel)obj;
            switch (_parentViewModel.SortColumn)
            {
                case "Cardinality":
                    return RowsCount.CompareTo(objTable.RowsCount) * _parentViewModel.SortDirection;
                case "Columns":
                    return ColumnsTotalSize.CompareTo(objTable.ColumnsTotalSize) * _parentViewModel.SortDirection;
                case "Dictionary":
                    return ColumnsDictionarySize.CompareTo(objTable.ColumnsDictionarySize) * _parentViewModel.SortDirection;
                case "Data":
                    return ColumnsDataSize.CompareTo(objTable.ColumnsDataSize) * _parentViewModel.SortDirection;
                default:
                    return TableName.CompareTo(objTable.TableName) * _parentViewModel.SortDirection;
            }
            
        }

        public bool IsExpanded {get;set;}
        public long RelationshipMaxToCardinality { get; }
    }
   
}
