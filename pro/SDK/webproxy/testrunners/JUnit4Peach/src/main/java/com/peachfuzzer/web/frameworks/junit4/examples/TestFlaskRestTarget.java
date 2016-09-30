package com.peachfuzzer.web.frameworks.junit4.examples;

import com.mashape.unirest.http.HttpResponse;
import com.mashape.unirest.http.JsonNode;
import com.mashape.unirest.http.Unirest;
import junit.framework.Assert;
import org.apache.http.HttpHost;
import org.json.JSONObject;
import org.junit.After;
import org.junit.Before;
import org.junit.Test;

/**
 * Example JUnit4 tests for Flask Rest Target
 */
public class TestFlaskRestTarget {
    
    public String _baseUrl = "http://127.0.0.1:5000";
    public int _lastUserId = 2;
    
    @Before
    public void setup() throws Throwable
    {
        _lastUserId = 2;
        
        Unirest
            .delete(String.format("%s/api/users/%s", _baseUrl, _lastUserId))
            .asString();
        Unirest
            .delete(String.format("%s/api/users?user=dd", _baseUrl))
            .asString();

    }
    
    @After
    public void teardown() throws Throwable
    {
        Unirest
            .delete(String.format("%s/api/users/%s", _baseUrl, _lastUserId))
            .asString();
    }
    
    @Test
    public void getAllUsers() throws Throwable
    {
        Unirest
            .get(String.format("%s/api/users", _baseUrl))
            .asString();
    }
    
    @Test
    public void createUser() throws Throwable
    {
        JSONObject user = new JSONObject();
        user.put("user", "dd");
        user.put("first", "mike");
        user.put("last", "smith");
        user.put("password", "fnord");
        
        HttpResponse<JsonNode> ret;
        ret = Unirest
            .post(String.format("%s/api/users", _baseUrl))
            .header("Content-Type", "application/json")
            .body(user)
            .asJson();
        
        Assert.assertEquals(201, ret.getStatus());
        _lastUserId = ret.getBody().getObject().getInt("user_id");
    }
    
    @Test
    public void updateUser() throws Throwable
    {
        JSONObject user = new JSONObject();
        user.put("user", "dd");
        user.put("first", "mike");
        user.put("last", "smith");
        user.put("password", "fnord");
        
        HttpResponse<JsonNode> ret;
        ret = Unirest
            .post(String.format("%s/api/users", _baseUrl))
            .header("Content-Type", "application/json")
            .body(user)
            .asJson();
        
        Assert.assertEquals(201, ret.getStatus());
        _lastUserId = ret.getBody().getObject().getInt("user_id");
        
        user = new JSONObject();
        user.put("user", "dd");
        user.put("first", "john");
        user.put("last", "smith");
        user.put("password", "fn0rd");
        
        HttpResponse<String> ret2;
        ret2 = Unirest
            .put(String.format("%s/api/users/%s", _baseUrl, _lastUserId))
            .header("Content-Type", "application/json")
            .body(user)
            .asString();
        
        Assert.assertEquals(204, ret2.getStatus());
    }
}
