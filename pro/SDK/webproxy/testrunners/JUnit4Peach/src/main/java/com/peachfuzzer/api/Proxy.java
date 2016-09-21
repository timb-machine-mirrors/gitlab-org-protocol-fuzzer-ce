
package com.peachfuzzer.api;

import com.mashape.unirest.http.HttpResponse;
import com.mashape.unirest.http.JsonNode;
import com.mashape.unirest.http.Unirest;
import com.mashape.unirest.http.exceptions.UnirestException;
import com.mashape.unirest.http.options.Option;
import com.mashape.unirest.http.options.Options;
import java.util.logging.Level;
import java.util.logging.Logger;
import org.json.JSONArray;
import org.json.JSONObject;

/**
 * Wrapper for /p/proxy Peach API
 * 
 * @author Peach Fuzzer, LLC
 */
public class Proxy {
    
    private String _api = "http://127.0.0.1:8888";
    private String _jobid = "auto";
    private Object _unirestProxy = null;
    
    /**
     * Create Proxy instance with default values for api and jobid.
     * api defaults to "http://127.0.0.1:8888" and jobid defaults
     * to "auto".
     */
    public Proxy()
    {
    }
    
    /**
     * Create Proxy instance with default jobid of "auto".
     * @param api Base URL for Peach API. Example: "http://127.0.0.1:8888"
     */
    public Proxy(String api)
    {
        _api = api;
    }
    
    /**
     * Create Proxy instance
     * 
     * @param api Base URL for Peach API. Example: "http://127.0.0.1:8888"
     * @param jobid Specify jobid GUID or "auto" to discover.
     */
    public Proxy(String api, String jobid)
    {
        _api = api;
        _jobid = jobid;
    }
    
    protected void stashUnirestProxy()
    {
        _unirestProxy = Options.getOption(Option.PROXY);
    }
    
    protected void revertUnirestProxy()
    {
        Options.setOption(Option.PROXY, _unirestProxy);
    }
    
    protected void getJobId() throws PeachApiException
    {
        // Skip if we already know job id
        if(!_jobid.equals("auto"))
            return;
        
        stashUnirestProxy();
        try
        {
            HttpResponse<JsonNode> ret = null;
            try {
                ret = Unirest.get(String.format("%s/p/jobs?dryrun=false&running=true", _api)).asJson();
            } catch (UnirestException ex) {
                Logger.getLogger(Proxy.class.getName()).log(Level.SEVERE, "jobId error contacting Peach API", ex);
                throw new PeachApiException(
                        String.format("Error, exception contacting Peach API: %s", ex.getMessage()), ex);
            }

            if(ret == null)
            {
                throw new PeachApiException("Error, in Proxy.getJobId: ret was null", null);
            }

            if(ret.getStatus() != 200)
            {
                String errorMsg = String.format("Error, /p/jobs returned status code of %s: %s", 
                        ret.getStatus(), ret.getStatusText());

                throw new PeachApiException(errorMsg, null);
            }

            JSONArray array = ret.getBody().getArray();
            if(array.length() == 0)
            {
                throw new PeachApiException("Error, /p/jobs returned no running jobs.", null);
            }

            _jobid = array.getJSONObject(0).getString("id");
        }
        finally
        {
            revertUnirestProxy();
        }
    }
    
    /**
     * Notify Peach Proxy that a test session is starting.
     * 
     * Called ONCE at start of testing.
     * 
     * @throws PeachApiException
     */
    public void sessionSetup() throws PeachApiException
    {
        getJobId();
        stashUnirestProxy();
        
        try
        {
            HttpResponse<String> ret = null;
            try {
                ret = Unirest
                        .put(String.format("%s/p/proxy/%s/sessionSetup", _api, _jobid))
                        .asString();
            } catch (UnirestException ex) {
                Logger.getLogger(Proxy.class.getName()).log(Level.SEVERE, "Error contacting Peach API", ex);
                throw new PeachApiException(
                        String.format("Error, exception contacting Peach API: %s", ex.getMessage()), ex);
            }

            if(ret == null)
            {
                throw new PeachApiException("Error, in Proxy.sessionSetup: ret was null", null);
            }

            if(ret.getStatus() != 200)
            {
                String errorMsg = String.format("Error, /p/proxy/{id}/sessionSetup returned status code of %s: %s", 
                        ret.getStatus(), ret.getStatusText());

                throw new PeachApiException(errorMsg, null);
            }
        }
        finally
        {
            revertUnirestProxy();
        }
    }
    
    /**
     * Notify Peach Proxy that a test session is ending.
     * 
     * Called ONCE at end of testing. This will cause Peach to stop.
     * 
     * @throws PeachApiException
     */
    public void sessionTearDown() throws PeachApiException
    {
        getJobId();
        stashUnirestProxy();

        try
        {
            HttpResponse<String> ret = null;
            try {
                ret = Unirest
                        .put(String.format("%s/p/proxy/%s/sessionTearDown", _api, _jobid))
                        .asString();
            } catch (UnirestException ex) {
                Logger.getLogger(Proxy.class.getName()).log(Level.SEVERE, "Error contacting Peach API", ex);
                throw new PeachApiException(
                        String.format("Error, exception contacting Peach API: %s", ex.getMessage()), ex);
            }

            if(ret == null)
            {
                throw new PeachApiException("Error, in Proxy.sessionTearDown: ret was null", null);
            }

            if(ret.getStatus() != 200)
            {
                String errorMsg = String.format("Error, /p/proxy/{id}/sessionTearDown returned status code of %s: %s", 
                        ret.getStatus(), ret.getStatusText());

                throw new PeachApiException(errorMsg, null);
            }
        }
        finally
        {
            revertUnirestProxy();
        }
    }
    
    /**
     * Notify Peach Proxy that setup tasks are about to run.
     * 
     * This will disable fuzzing of messages so the setup tasks
     * always work OK.
     * 
     * @throws PeachApiException
     */
    public void testSetUp() throws PeachApiException
    {
        getJobId();
        stashUnirestProxy();
        
        try
        {
            HttpResponse<String> ret = null;
            try {
                ret = Unirest
                        .put(String.format("%s/p/proxy/%s/testSetUp", _api, _jobid))
                        .asString();
            } catch (UnirestException ex) {
                Logger.getLogger(Proxy.class.getName()).log(Level.SEVERE, "Error contacting Peach API", ex);
                throw new PeachApiException(
                        String.format("Error, exception contacting Peach API: %s", ex.getMessage()), ex);
            }

            if(ret == null)
            {
                throw new PeachApiException("Error, in Proxy.testSetUp: ret was null", null);
            }

            if(ret.getStatus() != 200)
            {
                String errorMsg = String.format("Error, /p/proxy/{id}/testSetUp returned status code of %s: %s", 
                        ret.getStatus(), ret.getStatusText());

                throw new PeachApiException(errorMsg, null);
            }
        }
        finally
        {
            revertUnirestProxy();
        }
    }
    
    /**
     * Notify Peach Proxy that teardown tasks are about to run.
     * 
     * This will disable fuzzing of messages so the teardown tasks
     * always work OK.
     * 
     * @throws PeachApiException
     */
    public void testTearDown() throws PeachApiException
    {
        getJobId();
        stashUnirestProxy();
        
        try
        {
            HttpResponse<String> ret = null;
            try {
                ret = Unirest
                        .put(String.format("%s/p/proxy/%s/testTearDown", _api, _jobid))
                        .asString();
            } catch (UnirestException ex) {
                Logger.getLogger(Proxy.class.getName()).log(Level.SEVERE, "Error contacting Peach API", ex);
                throw new PeachApiException(
                        String.format("Error, exception contacting Peach API: %s", ex.getMessage()), ex);
            }

            if(ret == null)
            {
                throw new PeachApiException("Error, in Proxy.testTearDown: ret was null", null);
            }

            if(ret.getStatus() != 200)
            {
                String errorMsg = String.format("Error, /p/proxy/{id}/testTearDown returned status code of %s: %s", 
                        ret.getStatus(), ret.getStatusText());

                throw new PeachApiException(errorMsg, null);
            }
        }
        finally
        {
            revertUnirestProxy();
        }
    }
    
    /**
     * Notify Peach Proxy that a test case is starting.
     * This will enable fuzzing and group all of the following
     * requests into a group.
     * 
     * @param name Name of test case
     * @throws PeachApiException
     */
    public void testCase(String name) throws PeachApiException
    {
        getJobId();
        stashUnirestProxy();

        try
        {
            HttpResponse<String> ret = null;

            try {
                JSONObject obj = new JSONObject();
                obj.put("name", name);

                ret = Unirest
                        .put(String.format("%s/p/proxy/%s/testCase", _api, _jobid))
                        .header("Content-Type", "application/json")
                        .body(obj)
                        .asString();
            } catch (UnirestException ex) {
                Logger.getLogger(Proxy.class.getName()).log(Level.SEVERE, "Error contacting Peach API", ex);
                throw new PeachApiException(
                        String.format("Error, exception contacting Peach API: %s", ex.getMessage()), ex);
            }

            if(ret == null)
            {
                throw new PeachApiException("Error, in Proxy.testCase: ret was null", null);
            }

            if(ret.getStatus() != 200)
            {
                String errorMsg = String.format("Error, /p/proxy/{id}/testCase returned status code of %s: %s", 
                        ret.getStatus(), ret.getStatusText());

                throw new PeachApiException(errorMsg, null);
            }
        }
        finally
        {
            revertUnirestProxy();
        }
    }
}
