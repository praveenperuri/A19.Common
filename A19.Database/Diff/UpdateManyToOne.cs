using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using A19.Core;

namespace A19.Database.Diff
{
    public class UpdateManyToOne<
        TNew,
        TDbValue,
        TNewProp,
        TDbProp,
        TKey,
        TChildKey,
        TUserId> : 
        BaseNode<TNewProp, TDbProp, TChildKey, TUserId>,
        IUpdateManyToOne<TNew, TDbValue, TKey, TUserId> 
        where TDbValue : AbstractDatabaseRecord<TKey, TNew>
        where TDbProp : AbstractDatabaseRecord<TChildKey, TNewProp>, new()
        where TNewProp : class

    {
        private readonly bool immutable;
        
        private readonly Func<TNew, TNewProp> newProp;

        private readonly Func<TDbValue, TDbProp> dbValue;

        private readonly Action<TDbValue, TDbProp> setDbValue;

        private readonly IEnumerable<IUpdateValue<TNewProp, TDbProp, TChildKey>> childValues;

        private readonly IDiffRepository<TUserId, TChildKey, TDbProp, TNewProp> diffRepository;

        public UpdateManyToOne(
            int nodeId,
            bool immutable,
            Func<TNew, TNewProp> newProp,
            Expression<Func<TDbValue, TDbProp>> dbValue,
            IEnumerable<IUpdateValue<TNewProp, TDbProp, TChildKey>> childValues,
            IDiffRepository<TUserId, TChildKey, TDbProp, TNewProp> diffRepository) : base(nodeId)
        {
            this.immutable = immutable;
            this.newProp = newProp;
            this.dbValue = dbValue.Compile();
            this.setDbValue = ExpressionUtils.CreateSetter(dbValue);
            this.childValues = childValues;
            this.diffRepository = diffRepository;
        }

        public bool Immutable => this.immutable;

        public bool Update(
            TNew newValue,
            TDbValue value,
            Dictionary<int, IUpdateRecords<TUserId>> updateValues)
        {
            var newModel = this.newProp(newValue);
            var dbValue = this.dbValue(value);
            if (newModel == null)
            {
                this.setDbValue(value, null);
                return true;
            }

            if (dbValue == null)
            {
                dbValue = new TDbProp();
            }

            var changed = false;
            foreach (var updateValue in childValues)
            {
                changed = updateValue.Update(newModel, dbValue) || changed;
            }

            // Need to run this first.
            changed = this.RunManyToOneUpdate(
                newModel,
                dbValue,
                updateValues) || changed;
            if (changed)
            {
                UpdateRecordImpl<TUserId, TDbProp, TChildKey, TNewProp> updateNode;
                if (updateValues.TryGetValue(this.NodeId, out var node))
                {
                    updateNode = (UpdateRecordImpl<TUserId, TDbProp, TChildKey, TNewProp>) node;
                }
                else
                {
                    updateNode = new UpdateRecordImpl<TUserId, TDbProp, TChildKey, TNewProp>(this.NodeId, this.diffRepository);
                    updateValues[this.NodeId] = updateNode;
                }

                if (this.immutable)
                {
                    updateNode.Add(dbValue);
                    dbValue.NewRecord();
                }
                else
                {
                    updateNode.Update(dbValue);
                    dbValue.UpdateRecord();
                }
            }

            changed = this.RunManyToManyUpdate(
                          newModel,
                          dbValue,
                          updateValues) || changed;
            return changed;
        }
    }
}