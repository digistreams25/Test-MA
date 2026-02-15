using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using TwinThread.Civil3D.LOI.Core;
using TwinThread.Civil3D.LOI.Utilities;

namespace TwinThread.Civil3D.LOI.Discovery
{
    /// <summary>
    /// Discovers and extracts context from Civil 3D objects
    /// </summary>
    public class C3DDiscovery
    {
        /// <summary>
        /// Discover all target objects in the drawing
        /// </summary>
        public List<EntityContext> DiscoverAll(CivilDocument civilDoc, Document acDoc)
        {
            List<EntityContext> contexts = new List<EntityContext>();

            // Discover corridors
            contexts.AddRange(DiscoverCorridors(civilDoc));

            // Discover pipes and structures
            contexts.AddRange(DiscoverPipesAndStructures(civilDoc));

            // Discover 3D solids
            contexts.AddRange(DiscoverSolids(acDoc));

            return contexts;
        }

        /// <summary>
        /// Discover corridors
        /// </summary>
        private List<EntityContext> DiscoverCorridors(CivilDocument civilDoc)
        {
            List<EntityContext> contexts = new List<EntityContext>();

            try
            {
                if (civilDoc.CorridorCollection == null)
                    return contexts;

                using (Transaction tr = civilDoc.Database.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId corridorId in civilDoc.CorridorCollection)
                    {
                        try
                        {
                            Corridor corridor = tr.GetObject(corridorId, OpenMode.ForWrite) as Corridor;
                            if (corridor == null)
                                continue;

                            EntityContext ctx = new EntityContext
                            {
                                ObjectId = corridorId,
                                Handle = corridor.Handle,
                                ObjectType = Constants.ObjectTypeCorridor,
                                Layer = corridor.Layer,
                                Name = corridor.Name,
                                StyleName = GetCorridorStyleName(corridor),
                                AlignmentName = GetCorridorAlignmentName(corridor),
                                AssemblyName = GetCorridorAssemblyName(corridor),
                                RegionName = GetCorridorRegionNames(corridor),
                                IsLocked = corridor.IsWriteEnabled == false
                            };

                            contexts.Add(ctx);
                        }
                        catch (Exception ex)
                        {
                            // Report error but continue
                            contexts.Add(new EntityContext
                            {
                                ObjectId = corridorId,
                                ObjectType = Constants.ObjectTypeCorridor,
                                ErrorMessage = $"Property access failed: {ex.Message}"
                            });
                        }
                    }

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                // Collection access failed
                contexts.Add(new EntityContext
                {
                    ObjectType = Constants.ObjectTypeCorridor,
                    ErrorMessage = $"Corridor collection access failed: {ex.Message}"
                });
            }

            return contexts;
        }

        /// <summary>
        /// Discover pipes and structures from all networks
        /// </summary>
        private List<EntityContext> DiscoverPipesAndStructures(CivilDocument civilDoc)
        {
            List<EntityContext> contexts = new List<EntityContext>();

            try
            {
                using (Transaction tr = civilDoc.Database.TransactionManager.StartTransaction())
                {
                    ObjectIdCollection networkIds = civilDoc.GetPipeNetworkIds();
                    if (networkIds == null)
                        return contexts;

                    foreach (ObjectId networkId in networkIds)
                    {
                        try
                        {
                            Network network = tr.GetObject(networkId, OpenMode.ForRead) as Network;
                            if (network == null)
                                continue;

                            string networkName = network.Name;

                            // Discover pipes
                            foreach (ObjectId pipeId in network.GetPipeIds())
                            {
                                contexts.Add(ExtractPipeContext(tr, pipeId, networkName));
                            }

                            // Discover structures
                            foreach (ObjectId structureId in network.GetStructureIds())
                            {
                                contexts.Add(ExtractStructureContext(tr, structureId, networkName));
                            }
                        }
                        catch (Exception ex)
                        {
                            contexts.Add(new EntityContext
                            {
                                ObjectId = networkId,
                                ErrorMessage = $"Network access failed: {ex.Message}"
                            });
                        }
                    }

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                contexts.Add(new EntityContext
                {
                    ErrorMessage = $"Pipe network discovery failed: {ex.Message}"
                });
            }

            return contexts;
        }

        /// <summary>
        /// Extract context from a pipe
        /// </summary>
        private EntityContext ExtractPipeContext(Transaction tr, ObjectId pipeId, string networkName)
        {
            try
            {
                Pipe pipe = tr.GetObject(pipeId, OpenMode.ForWrite) as Pipe;
                if (pipe == null)
                    return new EntityContext { ObjectId = pipeId, ErrorMessage = "Failed to open pipe" };

                return new EntityContext
                {
                    ObjectId = pipeId,
                    Handle = pipe.Handle,
                    ObjectType = Constants.ObjectTypePipe,
                    Layer = pipe.Layer,
                    Name = pipe.Name ?? pipe.Handle.ToString(),
                    StyleName = GetPipeStyleName(pipe),
                    NetworkName = networkName,
                    PartFamily = GetPartFamily(pipe),
                    PartSize = GetPartSize(pipe),
                    IsLocked = pipe.IsWriteEnabled == false
                };
            }
            catch (Exception ex)
            {
                return new EntityContext
                {
                    ObjectId = pipeId,
                    ObjectType = Constants.ObjectTypePipe,
                    ErrorMessage = $"Pipe property access failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Extract context from a structure
        /// </summary>
        private EntityContext ExtractStructureContext(Transaction tr, ObjectId structureId, string networkName)
        {
            try
            {
                Structure structure = tr.GetObject(structureId, OpenMode.ForWrite) as Structure;
                if (structure == null)
                    return new EntityContext { ObjectId = structureId, ErrorMessage = "Failed to open structure" };

                return new EntityContext
                {
                    ObjectId = structureId,
                    Handle = structure.Handle,
                    ObjectType = Constants.ObjectTypeStructure,
                    Layer = structure.Layer,
                    Name = structure.Name ?? structure.Handle.ToString(),
                    StyleName = GetStructureStyleName(structure),
                    NetworkName = networkName,
                    PartFamily = GetPartFamily(structure),
                    PartSize = GetPartSize(structure),
                    IsLocked = structure.IsWriteEnabled == false
                };
            }
            catch (Exception ex)
            {
                return new EntityContext
                {
                    ObjectId = structureId,
                    ObjectType = Constants.ObjectTypeStructure,
                    ErrorMessage = $"Structure property access failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Discover 3D solids
        /// </summary>
        private List<EntityContext> DiscoverSolids(Document acDoc)
        {
            List<EntityContext> contexts = new List<EntityContext>();

            try
            {
                using (Transaction tr = acDoc.Database.TransactionManager.StartTransaction())
                {
                    BlockTable bt = tr.GetObject(acDoc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                    foreach (ObjectId objId in btr)
                    {
                        try
                        {
                            Entity ent = tr.GetObject(objId, OpenMode.ForWrite) as Entity;
                            if (ent is Solid3d solid)
                            {
                                contexts.Add(new EntityContext
                                {
                                    ObjectId = objId,
                                    Handle = solid.Handle,
                                    ObjectType = Constants.ObjectTypeSolid,
                                    Layer = solid.Layer,
                                    Name = solid.Handle.ToString(), // Solids don't have names
                                    IsLocked = solid.IsWriteEnabled == false
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            contexts.Add(new EntityContext
                            {
                                ObjectId = objId,
                                ObjectType = Constants.ObjectTypeSolid,
                                ErrorMessage = $"Solid access failed: {ex.Message}"
                            });
                        }
                    }

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                contexts.Add(new EntityContext
                {
                    ObjectType = Constants.ObjectTypeSolid,
                    ErrorMessage = $"Solid discovery failed: {ex.Message}"
                });
            }

            return contexts;
        }

        // Helper methods for extracting properties safely

        private string GetCorridorStyleName(Corridor corridor)
        {
            try { return corridor.StyleName; }
            catch { return ""; }
        }

        private string GetCorridorAlignmentName(Corridor corridor)
        {
            try
            {
                if (corridor.BaselineCount > 0)
                {
                    var baseline = corridor.Baselines[0];
                    return baseline?.AlignmentName ?? "";
                }
            }
            catch { }
            return "";
        }

        private string GetCorridorAssemblyName(Corridor corridor)
        {
            try
            {
                if (corridor.BaselineCount > 0)
                {
                    var baseline = corridor.Baselines[0];
                    if (baseline != null && baseline.BaselineRegions.Count > 0)
                    {
                        return baseline.BaselineRegions[0]?.AssemblyName ?? "";
                    }
                }
            }
            catch { }
            return "";
        }

        private string GetCorridorRegionNames(Corridor corridor)
        {
            try
            {
                List<string> regionNames = new List<string>();
                if (corridor.BaselineCount > 0)
                {
                    var baseline = corridor.Baselines[0];
                    if (baseline != null)
                    {
                        foreach (BaselineRegion region in baseline.BaselineRegions)
                        {
                            regionNames.Add(region.Name);
                        }
                    }
                }
                return string.Join(";", regionNames);
            }
            catch { }
            return "";
        }

        private string GetPipeStyleName(Pipe pipe)
        {
            try { return pipe.StyleName; }
            catch { return ""; }
        }

        private string GetStructureStyleName(Structure structure)
        {
            try { return structure.StyleName; }
            catch { return ""; }
        }

        private string GetPartFamily(Pipe pipe)
        {
            try { return pipe.PartFamilyName ?? ""; }
            catch { return ""; }
        }

        private string GetPartFamily(Structure structure)
        {
            try { return structure.PartFamilyName ?? ""; }
            catch { return ""; }
        }

        private string GetPartSize(Pipe pipe)
        {
            try { return pipe.PartSizeName ?? ""; }
            catch { return ""; }
        }

        private string GetPartSize(Structure structure)
        {
            try { return structure.PartSizeName ?? ""; }
            catch { return ""; }
        }
    }
}
