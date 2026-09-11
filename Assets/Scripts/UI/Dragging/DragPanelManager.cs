using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
[System.Serializable]
public class DragFolderTree
{
    public string id;
    public List<string> nodes;
}
[System.Serializable]
public class DragFolder
{
    public string id;
    public string name;
}
public class DragPanelManager : Singleton<DragPanelManager>
{
    [SerializeField]
    private GameObject folderPrefab;
    [HideInInspector]
    public Transform originalObject;
    [HideInInspector]
    public bool isDragging;
    [HideInInspector]
    public DragFolderObj CurrentDropTarget;
    public UnityEvent onReorder;
    public UnityEvent onLoad;
    public UnityEvent onSave;
    public UnityEvent onNavigate;
    private List<DragFolderTree> dragTrees = new List<DragFolderTree>();
    private List<DragFolder> dragFolders = new List<DragFolder>();
    private const string unverifiedString = "unverified:";
    private const string folderString = "f:";
    private const string objString = "o:";
    private Dictionary<Transform, string> confirmedParents = new Dictionary<Transform, string>();
    private Dictionary<string, Transform> treeContainers = new Dictionary<string, Transform>();
    private Dictionary<string, string> currentPaths = new Dictionary<string, string>();
    private Dictionary<string, Transform> liveEntries = new Dictionary<string, Transform>();
    private List<string> pendingRefresh = new List<string>();
    private bool loaded;
    public void OnEnable()
    {
        if (!loaded)
        {
            Load();
        }
        CancelInvoke(nameof(Save));
        InvokeRepeating(nameof(Save), 2f, 2f);
    }
    private void OnDisable()
    {
        CancelInvoke(nameof(Save));
        Save();
    }
    private void LateUpdate()
    {
        if (pendingRefresh.Count == 0) return;
        for (int i = 0; i < pendingRefresh.Count; i++)
        {
            Refresh(pendingRefresh[i]);
        }
        pendingRefresh.Clear();
    }
    public void Save()
    {
        ES3.Save("dragTrees", dragTrees);
        ES3.Save("dragFolders", dragFolders);
        onSave?.Invoke();
    }
    public void Load()
    {
        if (ES3.KeyExists("dragTrees"))
        {
            dragTrees = ES3.Load<List<DragFolderTree>>("dragTrees");
        }
        if (ES3.KeyExists("dragFolders"))
        {
            dragFolders = ES3.Load<List<DragFolder>>("dragFolders");
        }
        if (dragTrees == null)
        {
            dragTrees = new List<DragFolderTree>();
        }
        if (dragFolders == null)
        {
            dragFolders = new List<DragFolder>();
        }
        foreach (DragFolderTree tree in dragTrees)
        {
            if (tree.nodes == null)
            {
                tree.nodes = new List<string>();
            }
        }
        loaded = true;
        onLoad?.Invoke();
    }
    public void LoadTree(Transform tree, string id)
    {
        if (tree == null || string.IsNullOrEmpty(id)) return;
        if (folderPrefab == null)
        {
            Debug.LogError("[DragManager] No folder prefab assigned.");
            return;
        }
        if (!VerifyParent(tree)) return;
        DragFolderTree data = GetTree(id);
        RegisterContainer(tree, id);
        if (!currentPaths.ContainsKey(id))
        {
            currentPaths[id] = string.Empty;
        }
        foreach (string entry in data.nodes)
        {
            if (!IsFolderEntry(entry)) continue;
            string folderId = GetLastId(GetPath(entry));
            if (liveEntries.ContainsKey(folderId) && liveEntries[folderId] != null) continue;
            SpawnFolder(id, folderId, tree);
        }
        RequestRefresh(id);
        onLoad?.Invoke();
    }
    public void MakeObjectDragable(GameObject obj, string folderData = "")
    {
        if (obj == null) return;
        Transform parent = obj.transform.parent;
        if (VerifyParent(parent, folderData))
        {
            string treeId = GetContainerTree(parent);
            if (string.IsNullOrEmpty(treeId)) return;
            DragFolderTree tree = GetTree(treeId);
            string objId = GetObjId(folderData);
            if (string.IsNullOrEmpty(objId))
            {
                objId = System.Guid.NewGuid().ToString();
            }
            if (FindEntry(tree, objId) < 0)
            {
                InsertEntry(tree, objString, GetCurrentPath(treeId), objId, int.MaxValue);
            }
            AddDragAble(obj, objId, treeId);
            liveEntries[objId] = obj.transform;
            RequestRefresh(treeId);
        }
    }
    public string CreateFolder(Transform parent)
    {
        if (VerifyParent(parent))
        {
            string treeId = GetContainerTree(parent);
            if (string.IsNullOrEmpty(treeId)) return string.Empty;
            DragFolderTree tree = GetTree(treeId);
            string id = System.Guid.NewGuid().ToString();
            GetFolder(id);
            InsertEntry(tree, folderString, GetCurrentPath(treeId), id, int.MaxValue);
            SpawnFolder(treeId, id, parent);
            RequestRefresh(treeId);
            onReorder?.Invoke();
            return id;
        }
        return string.Empty;
    }
    public string CreateTree(Transform tree)
    {
        if (VerifyParent(tree))
        {
            string current = confirmedParents[tree];
            if (current.Contains(unverifiedString))
            {
                string id = System.Guid.NewGuid().ToString();
                PromoteTree(tree, id);
                return id;
            }
            Debug.LogError("[DragManager] Cannot create existing tree into tree.");
            return current;
        }
        return string.Empty;
    }
    public void EnterFolder(string folderId)
    {
        DragFolderTree tree = FindEntryTree(folderId, out int index);
        if (tree == null) return;
        if (!IsFolderEntry(tree.nodes[index]))
        {
            Debug.LogWarning("[DragManager] Entry is not a folder.");
            return;
        }
        currentPaths[tree.id] = GetPath(tree.nodes[index]);
        RequestRefresh(tree.id);
        onNavigate?.Invoke();
    }
    public void ExitFolder(Transform container)
    {
        if (container == null || !confirmedParents.ContainsKey(container)) return;
        ExitFolder(confirmedParents[container]);
    }
    public void ExitFolder(string treeId)
    {
        string current = GetCurrentPath(treeId);
        if (string.IsNullOrEmpty(current)) return;
        currentPaths[treeId] = GetParentPath(current);
        RequestRefresh(treeId);
        onNavigate?.Invoke();
    }
    public void MoveEntry(string entryId, int levelIndex)
    {
        DragFolderTree tree = FindEntryTree(entryId, out int index);
        if (tree == null)
        {
            Debug.LogWarning("[DragManager] Unknown entry " + entryId);
            return;
        }
        if (SetEntryPath(tree, index, GetParentPath(GetPath(tree.nodes[index])), levelIndex))
        {
            RequestRefresh(tree.id);
            onReorder?.Invoke();
        }
    }
    public void MoveIntoFolder(string entryId, string folderId)
    {
        if (entryId == folderId) return;
        DragFolderTree tree = FindEntryTree(entryId, out int index);
        if (tree == null)
        {
            Debug.LogWarning("[DragManager] Unknown entry " + entryId);
            return;
        }
        int folderIndex = FindEntry(tree, folderId);
        if (folderIndex < 0)
        {
            Debug.LogWarning("[DragManager] Folder is not part of the same tree.");
            return;
        }
        if (!IsFolderEntry(tree.nodes[folderIndex]))
        {
            Debug.LogWarning("[DragManager] Target is not a folder.");
            return;
        }
        if (SetEntryPath(tree, index, GetPath(tree.nodes[folderIndex]), int.MaxValue))
        {
            RequestRefresh(tree.id);
            onReorder?.Invoke();
        }
    }
    public void MoveToParentFolder(string entryId)
    {
        DragFolderTree tree = FindEntryTree(entryId, out int index);
        if (tree == null) return;
        string parentPath = GetParentPath(GetPath(tree.nodes[index]));
        if (string.IsNullOrEmpty(parentPath)) return;
        if (SetEntryPath(tree, index, GetParentPath(parentPath), int.MaxValue))
        {
            RequestRefresh(tree.id);
            onReorder?.Invoke();
        }
    }
    public void RemoveEntry(string entryId, bool destroyObjects = true)
    {
        DragFolderTree tree = FindEntryTree(entryId, out int index);
        if (tree == null) return;
        string removedPath = GetPath(tree.nodes[index]);
        List<string> block = ExtractBlock(tree, index);
        foreach (string entry in block)
        {
            string id = GetLastId(GetPath(entry));
            bool isFolder = IsFolderEntry(entry);
            if (isFolder)
            {
                for (int i = dragFolders.Count - 1; i >= 0; i--)
                {
                    if (dragFolders[i].id == id)
                    {
                        dragFolders.RemoveAt(i);
                    }
                }
            }
            if (liveEntries.ContainsKey(id))
            {
                Transform item = liveEntries[id];
                liveEntries.Remove(id);
                if (item != null && (isFolder || destroyObjects))
                {
                    Destroy(item.gameObject);
                }
                else if (item != null)
                {
                    item.gameObject.SetActive(true);
                }
            }
        }
        string current = GetCurrentPath(tree.id);
        if (current == removedPath || IsUnderPath(current, removedPath))
        {
            currentPaths[tree.id] = GetParentPath(removedPath);
            onNavigate?.Invoke();
        }
        RequestRefresh(tree.id);
        onReorder?.Invoke();
    }
    public void RenameFolder(string folderId, string newName)
    {
        GetFolder(folderId).name = newName;
        if (liveEntries.ContainsKey(folderId) && liveEntries[folderId] != null)
        {
            SetFolderLabel(liveEntries[folderId], newName);
        }
    }
    public string GetFolderName(string folderId)
    {
        return GetFolder(folderId).name;
    }
    public int GetLevelIndex(Transform item)
    {
        if (item == null || item.parent == null) return 0;
        Transform container = item.parent;
        int index = 0;
        for (int i = 0; i < container.childCount; i++)
        {
            Transform child = container.GetChild(i);
            if (child == item) break;
            if (child.gameObject.activeSelf)
            {
                index++;
            }
        }
        return index;
    }
    public string GetCurrentPath(string treeId)
    {
        if (currentPaths.ContainsKey(treeId)) return currentPaths[treeId];
        return string.Empty;
    }
    public string GetCurrentFolderId(string treeId)
    {
        return GetLastId(GetCurrentPath(treeId));
    }
    public bool IsAtRoot(string treeId)
    {
        return string.IsNullOrEmpty(GetCurrentPath(treeId));
    }
    public List<string> GetBreadcrumb(string treeId)
    {
        List<string> crumbs = new List<string>();
        string current = GetCurrentPath(treeId);
        if (string.IsNullOrEmpty(current)) return crumbs;
        string[] parts = current.Split('/');
        foreach (string part in parts)
        {
            crumbs.Add(part);
        }
        return crumbs;
    }
    public bool IsFolder(string entryId)
    {
        DragFolderTree tree = FindEntryTree(entryId, out int index);
        if (tree == null) return false;
        return IsFolderEntry(tree.nodes[index]);
    }
    public string GetObjId(string folderData)
    {
        if (string.IsNullOrEmpty(folderData)) return string.Empty;
        return folderData.Split('|')[0];
    }
    private void Refresh(string treeId)
    {
        Transform container = GetTreeContainer(treeId);
        if (container == null) return;
        DragFolderTree tree = GetTree(treeId);
        string current = GetCurrentPath(treeId);
        foreach (string entry in tree.nodes)
        {
            string entryPath = GetPath(entry);
            string id = GetLastId(entryPath);
            if (!liveEntries.ContainsKey(id)) continue;
            Transform item = liveEntries[id];
            if (item == null)
            {
                liveEntries.Remove(id);
                continue;
            }
            bool visible = GetParentPath(entryPath) == current;
            if (item.gameObject.activeSelf != visible)
            {
                item.gameObject.SetActive(visible);
            }
            if (visible)
            {
                if (item.parent != container)
                {
                    item.SetParent(container, false);
                }
                item.SetAsLastSibling();
            }
        }
    }
    private void RequestRefresh(string treeId)
    {
        if (string.IsNullOrEmpty(treeId)) return;
        if (pendingRefresh.Contains(treeId)) return;
        pendingRefresh.Add(treeId);
    }
    private Transform SpawnFolder(string treeId, string folderId, Transform container)
    {
        GameObject folder = Instantiate(folderPrefab, container);
        folder.name = folderId;
        AddDragAble(folder, folderId, treeId);
        liveEntries[folderId] = folder.transform;
        SetFolderLabel(folder.transform, GetFolder(folderId).name);
        return folder.transform;
    }
    private void SetFolderLabel(Transform folder, string name)
    {
        if (folder.TryGetComponent(out Text label))
        {
            label.text = name;
            return;
        }
        Text nested = folder.GetComponentInChildren<Text>(true);
        if (nested != null)
        {
            nested.text = name;
        }
    }
    private DragAble AddDragAble(GameObject obj, string entryId, string treeId)
    {
        DragAble drag;
        if (obj.TryGetComponent(out DragAble d))
        {
            drag = d;
        }
        else
        {
            drag = obj.AddComponent<DragAble>();
        }
        drag.folderData = entryId + "|" + treeId;
        return drag;
    }
    private bool SetEntryPath(DragFolderTree tree, int index, string newParentPath, int levelIndex)
    {
        string oldPath = GetPath(tree.nodes[index]);
        string entryId = GetLastId(oldPath);
        if (newParentPath == oldPath || IsUnderPath(newParentPath, oldPath))
        {
            Debug.LogError("[DragManager] Cannot move a folder into itself.");
            return false;
        }
        List<string> block = ExtractBlock(tree, index);
        string newPath = CombinePath(newParentPath, entryId);
        for (int i = 0; i < block.Count; i++)
        {
            block[i] = GetTag(block[i]) + newPath + GetPath(block[i]).Substring(oldPath.Length);
        }
        tree.nodes.InsertRange(GetInsertIndex(tree, newParentPath, levelIndex), block);
        return true;
    }
    private int InsertEntry(DragFolderTree tree, string tag, string parentPath, string id, int levelIndex)
    {
        int index = GetInsertIndex(tree, parentPath, levelIndex);
        tree.nodes.Insert(index, tag + CombinePath(parentPath, id));
        return index;
    }
    private int GetInsertIndex(DragFolderTree tree, string parentPath, int levelIndex)
    {
        int start = 0;
        if (!string.IsNullOrEmpty(parentPath))
        {
            start = -1;
            for (int i = 0; i < tree.nodes.Count; i++)
            {
                if (GetPath(tree.nodes[i]) == parentPath)
                {
                    start = i + 1;
                    break;
                }
            }
            if (start < 0) return tree.nodes.Count;
        }
        int found = 0;
        int end = start;
        for (int i = start; i < tree.nodes.Count; i++)
        {
            string path = GetPath(tree.nodes[i]);
            if (!string.IsNullOrEmpty(parentPath) && !IsUnderPath(path, parentPath)) break;
            if (GetParentPath(path) == parentPath)
            {
                if (found == levelIndex) return i;
                found++;
            }
            end = i + 1;
        }
        return end;
    }
    private List<string> ExtractBlock(DragFolderTree tree, int index)
    {
        string path = GetPath(tree.nodes[index]);
        List<string> block = new List<string>();
        block.Add(tree.nodes[index]);
        int i = index + 1;
        while (i < tree.nodes.Count && IsUnderPath(GetPath(tree.nodes[i]), path))
        {
            block.Add(tree.nodes[i]);
            i++;
        }
        tree.nodes.RemoveRange(index, block.Count);
        return block;
    }
    private int FindEntry(DragFolderTree tree, string entryId)
    {
        for (int i = 0; i < tree.nodes.Count; i++)
        {
            if (GetLastId(GetPath(tree.nodes[i])) == entryId) return i;
        }
        return -1;
    }
    private DragFolderTree FindEntryTree(string entryId, out int index)
    {
        if (!string.IsNullOrEmpty(entryId))
        {
            foreach (DragFolderTree tree in dragTrees)
            {
                int found = FindEntry(tree, entryId);
                if (found >= 0)
                {
                    index = found;
                    return tree;
                }
            }
        }
        index = -1;
        return null;
    }
    private DragFolderTree GetTree(string treeId)
    {
        foreach (DragFolderTree tree in dragTrees)
        {
            if (tree.id == treeId) return tree;
        }
        DragFolderTree created = new DragFolderTree();
        created.id = treeId;
        created.nodes = new List<string>();
        dragTrees.Add(created);
        return created;
    }
    private DragFolder GetFolder(string folderId)
    {
        foreach (DragFolder folder in dragFolders)
        {
            if (folder.id == folderId) return folder;
        }
        DragFolder created = new DragFolder();
        created.id = folderId;
        created.name = "New Folder";
        dragFolders.Add(created);
        return created;
    }
    private void RegisterContainer(Transform container, string treeId)
    {
        confirmedParents[container] = treeId;
        treeContainers[treeId] = container;
    }
    private Transform GetTreeContainer(string treeId)
    {
        if (treeContainers.ContainsKey(treeId)) return treeContainers[treeId];
        return null;
    }
    private string GetContainerTree(Transform container)
    {
        if (!confirmedParents.ContainsKey(container)) return string.Empty;
        string id = confirmedParents[container];
        if (id.Contains(unverifiedString))
        {
            id = CreateTree(container);
        }
        return id;
    }
    private void PromoteTree(Transform root, string id)
    {
        string old = confirmedParents[root];
        treeContainers.Remove(old);
        if (currentPaths.ContainsKey(old))
        {
            currentPaths[id] = currentPaths[old];
            currentPaths.Remove(old);
        }
        RegisterContainer(root, id);
        GetTree(id);
        if (!currentPaths.ContainsKey(id))
        {
            currentPaths[id] = string.Empty;
        }
    }
    private bool VerifyParent(Transform parent, string folderData = "")
    {
        if (parent == null) return false;
        if (confirmedParents.ContainsKey(parent))
        {
            if (confirmedParents[parent].Contains(unverifiedString) && (!string.IsNullOrEmpty(folderData)))
            {
                string id = GetTreeId(folderData);
                if (!string.IsNullOrEmpty(id) && !id.Contains(unverifiedString))
                {
                    PromoteTree(parent, id);
                }
            }
            return true;
        }
        else if (parent.TryGetComponent(out VerticalLayoutGroup verticalLayoutGroup))
        {
            string id = GetTreeId(folderData);
            if (string.IsNullOrEmpty(id))
            {
                id = unverifiedString + System.Guid.NewGuid().ToString();
            }
            else
            {
                GetTree(id);
            }
            RegisterContainer(parent, id);
            if (!currentPaths.ContainsKey(id))
            {
                currentPaths[id] = string.Empty;
            }
            return true;
        }
        else
        {
            Debug.LogWarning("[DragManager] Parent not eligible for dragging.");
            return false;
        }
    }
    public string GetTreeId(string folderData)
    {
        if (string.IsNullOrEmpty(folderData)) return string.Empty;
        string[] parts = folderData.Split('|');
        if (parts.Length < 2) return string.Empty;
        return parts[1].Split('/')[0];
    }
    private bool IsFolderEntry(string entry)
    {
        return entry.StartsWith(folderString);
    }
    private string GetTag(string entry)
    {
        return entry.Substring(0, 2);
    }
    private string GetPath(string entry)
    {
        return entry.Substring(2);
    }
    private string GetParentPath(string path)
    {
        int index = path.LastIndexOf('/');
        if (index < 0) return string.Empty;
        return path.Substring(0, index);
    }
    private string GetLastId(string path)
    {
        int index = path.LastIndexOf('/');
        if (index < 0) return path;
        return path.Substring(index + 1);
    }
    private string CombinePath(string parentPath, string id)
    {
        if (string.IsNullOrEmpty(parentPath)) return id;
        return parentPath + "/" + id;
    }
    private bool IsUnderPath(string path, string parentPath)
    {
        if (string.IsNullOrEmpty(parentPath)) return !string.IsNullOrEmpty(path);
        return path.StartsWith(parentPath + "/");
    }
}