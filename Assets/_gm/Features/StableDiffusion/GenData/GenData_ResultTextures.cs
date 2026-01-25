using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace spz {

	public enum TexturePreference { 
	    Unknown,
	    Tex2D, //a 'List<Texture2D>'. Useful for batches of projection art
	    Tex2DArray,  //a single RenderTexture, which has textures inside its slices. Useful for UDIMs
	}


	// Depending on texturePreference, will store either in the tex2D, or in  texArray+sliceIx
	// If your texture request failed (texture doesn't exist), all will be null.
	public class GenData_TextureRef{
	    public Guid guid; //of the texture itself, not of whole generation.
	    public TexturePreference texturePreference;
	    public Texture2D tex2D = null;
	    public RenderTexture texArray = null;
	    public int sliceIx = -1;
	    public UDIM_Sector udimSector = default;

	    public GenData_TextureRef(Guid guid, TexturePreference pref){ //custructor
	        this.guid = guid;
	        this.texturePreference=pref; 
	    }

	    public Vector2Int widthHeight(){
	        Texture t = tex_by_preference();
	        return t!=null?  new Vector2Int(t.width,t.height) : Vector2Int.zero;
	    }

	    //could be either Texture2D, or an arrray (RenderTexture)
	    public Texture tex_by_preference(){ 
	        switch (texturePreference){
	            case TexturePreference.Tex2D: return tex2D;
	            case TexturePreference.Tex2DArray: return texArray;
	        }
	        return null;
	    }
	}



	// Belongs to its 'GenData2D' object.
	// Receives generated images or updates with intermediate images.
	// Notifies event-subscribers when an image arives.
	public class GenData_ResultTextures{
	    public GenData2D genData { get; private set; } = null;
	    public TexturePreference preference() => _texturePreference.preference;
	    public Vector3Int widthHeight() => _texStorage.widthHeight();
	    public IReadOnlyList<Guid> orderedGuids => _texStorage.orderedGuids;
	    public int Count => _texStorage.Count;
	    public bool use_many_icons = false;//should all my textures be shown by several UI-elements or by single.

	    TexturePrefer _texturePreference = null;
	    TexturesStorage _texStorage  = null;
	    TexturesCallbacks _callbacks = null;
	    bool _isDisposed= false;

	    // CONSTRUCTOR
	    // begin_with: important when you plan to Update_PendingImages() later on.
	    // That method expects guids to already exists.
	    public GenData_ResultTextures( GenData2D myGenData, bool use_many_icons, IReadOnlyList<Guid> begin_with=null ){
	        this.genData = myGenData;
	        this.use_many_icons = use_many_icons;
	        _texturePreference = new TexturePrefer();
	        _texStorage = new TexturesStorage(this, begin_with);
	        _callbacks  = new TexturesCallbacks();
	        //NOTICE: don't fill the _textures2D, nor dont create texture-array yet.
	    }


	    public GenData_TextureRef GetTexture_ref( Guid texGuid )//might be null, if its icon was already deleted before.
	        => _texStorage.GetTexture_ref(texGuid);

	    //very expensive, might make a copy of all textures. Usually invoked when saving textures to disk
	    //destroyWhenDone_: tells if textures are a copy and have to be destroyed, or are a reference.
	    public Dictionary<Texture2D,UDIM_Sector> GetTextures2D_expensive(out bool destroyWhenDone_)
	        => _texStorage.GetTextures2D_expensive(out destroyWhenDone_);

	    public void Subscribe_for_TextureUpdates( Guid texGuid, Action<GenData_TextureRef> cb )
	        => _callbacks.Subscribe(texGuid, cb);

	    public void Unsubscribe_from_textureUpdates(Guid texGuid, Action<GenData_TextureRef> cb)
	        => _callbacks.Unsubscribe(texGuid, cb);

	    public void ForceEvent_OnGenerationCompleted()
	        => _callbacks.ForceEvent_OnGenerationCompleted(_texStorage);

	    // finds ix in our list
	    // destroys previous textures,
	    // updates with the new ones,
	    // notifies anyone who was using old textures.
	    public void Update_PendingImages(int iterIx, int numPerIter, string combinedTex_base64){//textures in a specific iteration

	        Texture2D combinedTexture = TextureTools_SPZ.Base64ToTexture(combinedTex_base64);
	        if(combinedTexture == null){ return; }
        
	        Texture2D[] subTextures = TextureTools_SPZ.Create_SubImages(combinedTexture, numPerIter);
	        Texture.DestroyImmediate(combinedTexture);

	        _texturePreference.Decide_Prefer_Texture2DList();
	        int from_ix = iterIx*numPerIter;
	        _texStorage.UpdateTextures_in_order(from_ix, subTextures, _texturePreference.preference);

	        _callbacks.NotifyAll(_texStorage);
	        //Don't destroy sub textures, they were accepted into list, or were already destroyed
	    }


	    // destroys previous textures,
	    // updates with the new ones,
	    // notifies anyone who was using old textures.
	    public void Finish_PendingImages(string[] base64_images){ //textures in all iterations

	        if(base64_images==null){ return; } //for example, if there was an error.

	        _texturePreference.Decide_Prefer_Texture2DList();
	        Texture2D[] subTextures =  base64_images.Select(b => TextureTools_SPZ.Base64ToTexture(b)).ToArray();
	        _texStorage.UpdateTextures_in_order(from_ix: 0, subTextures, _texturePreference.preference);
	        _callbacks.NotifyAll(_texStorage);
	        //Don't destroy sub textures, they were accepted into list, or were already destroyed
	    }//end()


   
	    public void Assign_TextureArray_Direct( RenderTexture textureArray_takeOwnership, 
	                                            IReadOnlyList<Guid> textureSlicesGuids,
	                                            IReadOnlyDictionary<Guid,UDIM_Sector> udims=null ){
        
	        Debug.Assert( textureArray_takeOwnership.dimension==TextureDimension.Tex2DArray, 
	                      $"{nameof(GenData_ResultTextures)}.{nameof(Assign_TextureArray_Direct)} expected a textureArray." );

	        _texturePreference.Decide_Prefer_TextureArray();
	        _texStorage.Assign_TextureArray_Direct(textureArray_takeOwnership, textureSlicesGuids, udims);
	        _callbacks.NotifyAll(_texStorage);
	    }

    
	    public void AssignTextures_Direct( bool clearAllExisting,
	                                       IReadOnlyDictionary<Guid,Texture2D> images_notOwnedByAnyone,
	                                       IReadOnlyDictionary<Guid,UDIM_Sector> udims=null ){
	        _texturePreference.Decide_Prefer_Texture2DList();
	        _texStorage.AssignTextures_Direct( clearAllExisting, images_notOwnedByAnyone, udims);
	        _callbacks.NotifyAll(_texStorage);
	    }


	    //for example, if user deleted some icon. Here we are releasing its texture from memory:
	    public void DisposeTexture( Guid textureGuid ){
	        if(_isDisposed){ return; }
	        _callbacks.Notify_WillRemove( textureGuid, _texStorage);
	        _texStorage.DisposeTexture( textureGuid );
	    }


	    public void Dispose(){
	        _callbacks.ForgetAll(_texStorage);
	        _callbacks.Dispose();
	        _texStorage.Dispose();
	        _texturePreference.Dispose();
	        _isDisposed = true;
	    }


	    public GenData_ResultTextures Clone(GenData2D genData_ofClone){
	        var clone =  (GenData_ResultTextures)this.MemberwiseClone();
	        clone.genData =  genData_ofClone;

	        clone._texturePreference = new TexturePrefer();
	        clone._texturePreference.Load(_texturePreference.preference);

	        clone._texStorage = _texStorage.Clone(clone); 
	        clone._callbacks  = new TexturesCallbacks();//don't copy callbacks.

	        clone.use_many_icons = use_many_icons;
	        return clone;
	    }


	    public GeneratedTextures_SL Save( StableProjectorz_SL spz ){

	        var sl = new GeneratedTextures_SL{  
	            textureFilepaths = new List<string>(), 
	        };
	        sl.texturePreference = _texturePreference.preference.ToString();
	        sl.use_many_icons =  use_many_icons;

	        _texStorage.Save(sl, spz.filepath_dataDir, genData.total_GUID.ToString());
	        return sl;
	    }//end()


	    public void Load( StableProjectorz_SL spz,  GeneratedTextures_SL texSL ){
	        Dispose();
	        _isDisposed = false;

	        TexturePreference texPref;
	        bool success =  Enum.TryParse( texSL.texturePreference, ignoreCase:true,  out texPref );
        
	        _texturePreference.Load( success? texPref : TexturePreference.Tex2D);
	        use_many_icons =  texSL.use_many_icons;

	        _texStorage.Load(texSL, spz.filepath_dataDir, _texturePreference.preference);

	        _callbacks = new TexturesCallbacks();
	    }//end()
	}



	class TexturesStorage{
    
	    GenData_ResultTextures owner;

	    List<Guid> _orderedGuids;
    
	    // We use either textureArray OR the dict of texture2D. (depends on owner.preference())
	    RenderTexture _textureArray = null;//for UV textures, every udim gets stored as a slice, into this texture-array.
	    Dictionary<Guid,Texture2D> _textures2D = null;//for projections, every batch (iteration) gets concatenated into this list.
    
	    Dictionary<Guid,UDIM_Sector> _udims = null; //optional. Might be null if not used.
    
	    public int Count => _orderedGuids.Count;
	    public IReadOnlyList<Guid> orderedGuids => _orderedGuids;

    
	    public Vector3Int widthHeight(){
	        if (_textureArray != null){  
	            return new Vector3Int(_textureArray.width, _textureArray.height, _textureArray.volumeDepth); 
	        }
	        if(_textures2D?.Count>0){
	            Texture tex = _textures2D.FirstOrDefault().Value;
	            return new Vector3Int(tex.width, tex.height, 1);
	        }
	        return default;
	    }

	    // CONSTRUCTOR.
	    // begin_with: important when you plan to UpdateTextures_in_order() later on.
	    // That method expects guids to already exists.
	    public TexturesStorage( GenData_ResultTextures owner,  IReadOnlyList<Guid> begin_with=null ){
	        this.owner = owner;
	        _orderedGuids = new List<Guid>();
	        _textures2D = null;
	        _textureArray = null;
	        _udims = null;
	        if(begin_with!=null){ _orderedGuids = begin_with.ToList(); }
	    }

	    // Might return entry with null textures, if requested icon was already forgotten before.
	    public GenData_TextureRef GetTexture_ref( Guid texGuid ){
        
	        var rslt = new GenData_TextureRef(texGuid, owner.preference());
	        _udims?.TryGetValue(texGuid, out rslt.udimSector);
        
	        switch (owner.preference()){
	            case TexturePreference.Tex2D:
	                _textures2D?.TryGetValue(texGuid, out rslt.tex2D);
	                return rslt;
	            case TexturePreference.Tex2DArray:
	                rslt.texArray = _textureArray;
	                rslt.sliceIx  = _orderedGuids.IndexOf(texGuid);
	                return rslt;
	        }
	        return rslt;
	    }

	    //very expensive, might make a copy of all textures. Usually invoked when saving textures to disk.
	    //destroyWhenDone_: tells if textures are a copy and have to be destroyed, or are a reference.
	    public Dictionary<Texture2D,UDIM_Sector> GetTextures2D_expensive(out bool destroyWhenDone_){
	        destroyWhenDone_ = true;
	        var dict = new Dictionary<Texture2D, UDIM_Sector>();

	        switch (owner.preference()){
	            case TexturePreference.Tex2D:
	                _textures2D ??= new Dictionary<Guid, Texture2D>();

	                for (int i=0; i<_orderedGuids.Count; ++i){
	                    Guid texGuid = _orderedGuids[i];
	                    UDIM_Sector udim = _udims != null ? _udims[texGuid] : default;
	                    dict.Add(_textures2D[texGuid], udim);
	                }
	                destroyWhenDone_ = false;
	                break;

	            case TexturePreference.Tex2DArray:
	                var arg =  owner.genData.kind == GenerationData_Kind.AmbientOcclusion?  TextureTools_SPZ.TexArr_toTex2dList_arg.RRR1 
	                                                                                      : TextureTools_SPZ.TexArr_toTex2dList_arg.Usual;
	                List<Texture2D> texList = TextureTools_SPZ.TextureArray_to_Texture2DList(_textureArray, arg);

	                for(int i=0; i<_orderedGuids.Count; ++i){
	                    Guid texGuid = _orderedGuids[i];
	                    UDIM_Sector udim = _udims != null ? _udims[texGuid] : default;
	                    dict.Add(texList[i], udim);
	                }
	                destroyWhenDone_ = true;
	                break;
	        }
	        return dict;
	    }



	    public TexturesStorage Clone( GenData_ResultTextures ownerOfClone ){
	        var clone =  new TexturesStorage(ownerOfClone);

	        int num = _orderedGuids.Count;

	        clone._orderedGuids = new List<Guid>(num);
	        for(int i=0; i<num; ++i){  clone._orderedGuids.Add(Guid.NewGuid()); }//a brand new guid!

	        if(_textures2D != null){ 
	            clone._textures2D  = new Dictionary<Guid, Texture2D>(num);
	            for(int i=0; i<num; ++i){
	                Guid myGuid    = _orderedGuids[i];
	                Guid cloneGuid = clone._orderedGuids[i];
	                if(_textures2D.TryGetValue(myGuid, out Texture2D myTex2D) == false){ continue; }
	                var cloneTex2D = TextureTools_SPZ.Clone_Tex2D(myTex2D);
	                clone._textures2D.Add( cloneGuid, cloneTex2D );
	            }
	        }

	        if(this._textureArray!=null){
	            clone._textureArray = TextureTools_SPZ.Clone_RenderTex( this._textureArray );
	        }

	        if(this._udims != null){
	            clone._udims =  new Dictionary<Guid, UDIM_Sector>(num);
	            for(int i=0; i<num; ++i){
	                Guid myGuid    = _orderedGuids[i];
	                Guid cloneGuid = clone._orderedGuids[i];
	                clone._udims.Add( cloneGuid,  new UDIM_Sector(_udims[myGuid].ToInt()) );
	            }
	        }
	        return clone;
	    }


	    public void Assign_TextureArray_Direct( RenderTexture textureArray_takeOwnership, 
	                                            IReadOnlyList<Guid> textureSlicesGuids,
	                                            IReadOnlyDictionary<Guid,UDIM_Sector> udimsDict=null ){

	        Debug.Assert( textureArray_takeOwnership.dimension==TextureDimension.Tex2DArray, 
	                      $"{nameof(GenData_ResultTextures)}.{nameof(Assign_TextureArray_Direct)} expected a textureArray." );

	        if(udimsDict != null){
	            bool numMatch =  udimsDict.Count==textureArray_takeOwnership.volumeDepth;
	            string msg =  $"The {nameof(udimsDict)} needs same count as volumeDepth of {nameof(textureArray_takeOwnership)}";
	            Debug.Assert(numMatch, msg);

	            bool keysMatch = textureSlicesGuids.OrderBy(g => g).SequenceEqual( udimsDict.Keys.OrderBy(k => k) );
	            msg = $"The keys in {nameof(textureSlicesGuids)} and {nameof(udimsDict)} must be identical.";
	            Debug.Assert(keysMatch, msg);
	        }

	        _orderedGuids = textureSlicesGuids.ToList();

	        if (_textureArray != null){  Texture.DestroyImmediate(_textureArray); }
	        _textureArray = textureArray_takeOwnership;
	        //_udims will be null, unless udimsDict isn't null:
	        _udims = udimsDict?.ToDictionary(kvp=>kvp.Key, kvp=>kvp.Value);
	    }

    
	    public void AssignTextures_Direct( bool clearAllExisting,
	                                       IReadOnlyDictionary<Guid,Texture2D> images_notOwnedByAnyone,
	                                       IReadOnlyDictionary<Guid,UDIM_Sector> udimsDict=null ){
	        if(udimsDict!=null){
	            bool keysMatch = images_notOwnedByAnyone.Keys.OrderBy(k => k).SequenceEqual(udimsDict.Keys.OrderBy(k => k));
	            string msg = $"The keys in {nameof(images_notOwnedByAnyone)} and {nameof(udimsDict)} must be identical.";
	            Debug.Assert(keysMatch, msg);
	        }

	        if (clearAllExisting){
	            Dispose();
	        }else {// Intersection with existing ordered GUIDs. 
	            //destroy our old textures that will be replaced. Keep the list entries.
	            var existingGuids = _orderedGuids.Intersect(images_notOwnedByAnyone.Keys).ToList();
	            foreach(var eg in existingGuids){  DisposeTexture(eg); }
	        }

	        _orderedGuids = _orderedGuids.Union(images_notOwnedByAnyone.Keys).ToList();

	        _textures2D ??=  new Dictionary<Guid,Texture2D>( images_notOwnedByAnyone.Count );
	        foreach(var kvp in images_notOwnedByAnyone){
	            _textures2D.UpdateOrAddValue(kvp.Key, kvp.Value); 
	        }

	        if (udimsDict != null) { 
	            _udims ??= new Dictionary<Guid, UDIM_Sector>( udimsDict.Count );
	            foreach(var kvp in udimsDict){ _udims.Add(kvp.Key, kvp.Value); }
	        }//else keep udims as is.

	        //don't destroy textures, they were accepted already.
	    }

    
	    //subTextures don't have owner. We'll accept or destroy them here.
	    public void UpdateTextures_in_order(int from_ix, Texture2D[] subTextures, TexturePreference texPref){
	        switch (texPref){
	            case TexturePreference.Tex2D:{
	                _textures2D ??= new Dictionary<Guid, Texture2D>();

	                for(int i=0; i< subTextures.Length; ++i){
	                    if(from_ix+i >= _orderedGuids.Count){ break; }
	                    Guid texGuid     = _orderedGuids[from_ix+i];
	                    Texture2D newTex =  subTextures[i]; //i, not from_ix+i

	                    Texture2D prevTex = _textures2D.GetOrAddValue(texGuid, ()=>null);
	                    if(prevTex != null){  Texture.Destroy(prevTex);  }
	                    _textures2D.UpdateOrAddValue(texGuid, newTex);
                    
	                    subTextures[i] = null;
	                }
	                //anything that remains an excess - has to be destroyed:
	                for(int i=0; i<subTextures.Length; ++i){ 
	                    Texture.Destroy( subTextures[i] );
	                }
	            }break;
	            case TexturePreference.Tex2DArray:{
	                var texSize =  new Vector2Int(subTextures[0].width, subTextures[0].height);
	                int numSlices_atLeast =  from_ix + _orderedGuids.Count;
	                //Makes sure the array is not null. Then fills required slice:
	                TextureTools_SPZ.TextureArray_EnsureSufficient(ref _textureArray, texSize, numSlices_atLeast);
	                TextureTools_SPZ.TextureArray_Fill_N_Slices( _textureArray,  subTextures.ToList(), from_ix );
	                //textures were copied into a slice of the texture array and is no longer needed.
	                subTextures.ToList().ForEach( t=>Texture.Destroy(t) );
	            }break;
	            default:
	                Debug.Assert(false, $"unknown texture preference, when doing ${nameof(UpdateTextures_in_order)}");
	            break;
	        }
	    }


	    public void DisposeTexture( Guid textureGuid ){
	        switch (owner.preference()){
	            case TexturePreference.Tex2D:
	                Texture2D oldTex = null;
	                _textures2D?.TryGetValue(textureGuid, out oldTex);
	                if(oldTex!=null){  Texture.DestroyImmediate(oldTex);  }

	                _textures2D?.Remove(textureGuid);
	                _orderedGuids.Remove(textureGuid);
	            break;
	            case TexturePreference.Tex2DArray:
	                int ix = _orderedGuids.IndexOf(textureGuid);
	                if (ix >= 0){ 
	                    TextureTools_SPZ.TextureArray_RemoveSlice( ref _textureArray, ix);
	                    _orderedGuids.RemoveAt(ix);
	                }
	            break;
	        }
	        _udims?.Remove(textureGuid);
	    }


	    public void Dispose(){
	        _textures2D?.DestroyImmediateAll();
	        if(_textureArray!=null){ 
	            Texture.DestroyImmediate(_textureArray);
	        }
	        _orderedGuids.Clear();
	        _textures2D?.Clear();
	        _udims?.Clear();
	    }


	    public void Save(GeneratedTextures_SL sl, string filepath_dataDir, string gen_internalGUID){

	        int num = _orderedGuids.Count;
	        sl.textureGuidsOrdered = _orderedGuids.Select( g=>g.ToString() ).ToList();

	        if (_textureArray != null){
	            List<Texture2D> tempList = TextureTools_SPZ.TextureArray_to_Texture2DList(_textureArray);
	            SaveTexturesList(tempList, sl, filepath_dataDir, gen_internalGUID);
	            tempList.ForEach( t=>Texture.DestroyImmediate(t) );
	        }
	        if(_textures2D != null){
	            var tempList = new List<Texture2D>(num);
	            foreach(Guid guid in _orderedGuids){  tempList.Add( _textures2D[guid] );  }

	            SaveTexturesList(tempList, sl, filepath_dataDir, gen_internalGUID);
	        }

	        if (_udims != null){
	            var tempList = new List<UDIM_Sector>(num);
	            foreach(Guid guid in _orderedGuids){  tempList.Add( _udims[guid] );  }
	            sl.udims = tempList;
	        }
	    }


	    public void Load( GeneratedTextures_SL texSL, string filepath_dataDir, TexturePreference texPref){
	        Dispose();//just in case

	        //load list of guids
	        _orderedGuids =  texSL.textureGuidsOrdered.Select( s=>new Guid(s) ).ToList();
	        int num = _orderedGuids.Count;

	        //load textures:
	        var format = GraphicsFormat.R8G8B8A8_UNorm;
	        List<Texture2D> texList = LoadTexturesList(texSL.textureFilepaths, filepath_dataDir, format);

	        switch (texPref){
	            case TexturePreference.Tex2D:{ 
	                _textures2D = new Dictionary<Guid, Texture2D>();
	                for(int i=0; i<num; ++i){
	                    _textures2D.Add( _orderedGuids[i], texList[i] );
	                }
	            }break;
	            case TexturePreference.Tex2DArray:{
	                if(texList.Count==0) break;
	                var texSize = new Vector2Int(texList[0].width, texList[0].height);
	                TextureTools_SPZ.TextureArray_EnsureSufficient( ref _textureArray, texSize,  texList.Count,  0, format);
	                TextureTools_SPZ.Texture2DList_to_TextureArray( ref _textureArray, texList, 0, format);
	                texList.ForEach( t=>Texture.DestroyImmediate(t) ); //was copied into slices, so delete textures.
	            }break;
	        }
	        //load udims:
	        _udims =  texSL.udims == null?  null : new Dictionary<Guid, UDIM_Sector>();
	        if(_udims!=null){ 
	            for(int i=0; i<num; ++i){ 
	                _udims.Add( _orderedGuids[i], texSL.udims[i] );
	            }
	        }
	    }

    
	    void SaveTexturesList(List<Texture2D> tex2DList, GeneratedTextures_SL sl, 
	                          string dataDir, string gen_internalGUID ){
	        for (int i=0; i<tex2DList.Count; i++){
	            Texture2D texture = tex2DList[i];
	            if (texture == null){
	                sl.textureFilepaths.Add("");
	                continue;
	            }
	            string pathInDataFolder = $"_tex_{i}_{gen_internalGUID}.png";
	            ProjectSaveLoad_Helper.Save_Tex2D_To_DataFolder(texture, dataDir, pathInDataFolder);

	            sl.textureFilepaths.Add(pathInDataFolder);
	        }
	    }

	    List<Texture2D> LoadTexturesList( List<string> filepaths, string dataDir, GraphicsFormat format ){
	        var list = new List<Texture2D>( filepaths.Count );
	        for(int i=0; i<filepaths.Count; ++i){
	            Texture2D tex = ProjectSaveLoad_Helper.Load_Texture2D_from_DataFolder( dataDir, filepaths[i],
	                                                                                   format, format);
	            list.Add(tex);
	        }
	        return list;
	    }

	}



	class TexturesCallbacks{
	    Dictionary<Guid, Action<GenData_TextureRef>> _actions  = new Dictionary<Guid, Action<GenData_TextureRef>>(8);
    
	    public TexturesCallbacks(){}//ctor
	    public void Dispose() => _actions.Clear();

	    public void Subscribe(Guid guid, Action<GenData_TextureRef> invokeThis){
	        if (_actions.ContainsKey(guid)){  
	            _actions[guid] += invokeThis; }
	        else{ 
	            _actions[guid] = invokeThis; }
	    }
	    public void Unsubscribe(Guid guid, Action<GenData_TextureRef> unsubThis){
	        if (_actions.ContainsKey(guid)==false){  return; }
	        _actions[guid] -= unsubThis;
	        if(_actions[guid]==null){ _actions.Remove(guid); }
	    }

	    public void Notify( Guid texGuid, TexturesStorage storage ){
	        if(!_actions.ContainsKey(texGuid)){ return; }
	        GenData_TextureRef texRef = storage.GetTexture_ref(texGuid);
	        try{  
	            _actions[texGuid]?.Invoke( texRef); }
	        catch(Exception ex){
	            Debug.LogError(ex);
	        }
	    }
	    public void NotifyAll( TexturesStorage storage ){
	        foreach(var kvp in _actions){
	            Guid guid = kvp.Key;
	            GenData_TextureRef texRef = storage.GetTexture_ref(guid);
	            try { 
	                kvp.Value?.Invoke(texRef);
	            }catch(Exception ex){
	                Debug.LogError(ex);
	            }
	        }//end foreach
	    }

	    public void Notify_WillRemove(Guid texGuid, TexturesStorage storage){
	        if(!_actions.ContainsKey(texGuid)){ return; }
	        GenData_TextureRef noTexRef = storage.GetTexture_ref(texGuid);
	        noTexRef.tex2D = null;
	        noTexRef.texArray = null;
	        try { 
	            _actions[texGuid]?.Invoke(noTexRef);
	        }catch(Exception ex){
	            Debug.LogError(ex);
	        }
	    }

	    public void ForgetAll(TexturesStorage storage){
	        foreach(var kvp in _actions){ Notify_WillRemove(kvp.Key, storage); }
	        _actions.Clear();
	    }

	    public void ForceEvent_OnGenerationCompleted( TexturesStorage storage ){
	        var guidKeys = _actions.Keys.ToList();//itere over COPY of keys, if someone unsubs during iterations.
	        foreach(var guidKey in guidKeys){
	            GenData_TextureRef texRef = storage.GetTexture_ref(guidKey);
	            try { 
	                _actions[guidKey]?.Invoke(texRef);
	            }catch(Exception ex){
	                Debug.LogError(ex);
	            }
	        }//end foreach
	    }

	}



	//tells whether textures are stored in a list, or in a RenderTexture which is a texture-array.
	class TexturePrefer {
	    public TexturePreference preference { get; private set; }

	    public TexturePrefer(){
	        preference = TexturePreference.Unknown;
	    }

	    public void Decide_Prefer_Texture2DList() {
	        bool isOk = preference == TexturePreference.Unknown 
	                    || preference == TexturePreference.Tex2D;
	        Debug.Assert(isOk, $"Changing texturePreference after it was already decided on");
	        preference = TexturePreference.Tex2D;
	    }

	    public void Decide_Prefer_TextureArray() {
	        bool isOk = preference == TexturePreference.Unknown 
	                    || preference == TexturePreference.Tex2DArray;
	        Debug.Assert(isOk, $"Changing texturePreference after it was already decided on");
	        preference = TexturePreference.Tex2DArray;
	    }

	    public void Load(TexturePreference pref) => this.preference=pref;
	    public void Dispose() => preference = TexturePreference.Unknown;
	}
}//end namespace
