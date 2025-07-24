# 🌩️ Cloudflare Setup Guide for stephentyrrell.ie

Complete step-by-step guide to get free SSL and performance benefits for your checklist generator.

## 🎯 **What You'll Get**

- ✅ **Free trusted SSL certificates** (no browser security warnings)
- ✅ **Global CDN** (faster loading worldwide)
- ✅ **DDoS protection** (enterprise-level security)
- ✅ **Analytics** (visitor insights)
- ✅ **Caching** (better performance)
- ✅ **Always Online** (backup if container goes down)

## 📋 **Step-by-Step Setup**

### **Step 1: Create Cloudflare Account**
1. Go to https://cloudflare.com
2. Click "Sign up" (it's free!)
3. Enter your email and create a password

### **Step 2: Add Your Domain**
1. Click "Add Site"
2. Enter: `stephentyrrell.ie`
3. Click "Add Site"
4. Choose **"Free Plan"** (perfect for your needs)
5. Click "Continue"

### **Step 3: Review DNS Records**
Cloudflare will scan your existing DNS records. You should see:
```
Type: CNAME
Name: checklist
Value: checklist-generator-1753371092.eastus.azurecontainer.io
```

If it's missing, add it:
1. Click "Add record"
2. Type: `CNAME`
3. Name: `checklist`
4. Target: `checklist-generator-1753371092.eastus.azurecontainer.io`
5. **Proxy status: Proxied** (orange cloud ☁️)
6. Click "Save"

### **Step 4: Update Nameservers**
Cloudflare will show you 2 nameservers like:
```
assigned-ns1.cloudflare.com
assigned-ns2.cloudflare.com
```

**Update these at your domain registrar:**
1. Log in to where you bought `stephentyrrell.ie`
2. Find "DNS Settings" or "Nameservers"
3. Replace existing nameservers with Cloudflare's
4. Save changes

**⏰ Wait time:** 5 minutes - 24 hours for full propagation

### **Step 5: Configure SSL (IMPORTANT!)**
1. In Cloudflare dashboard, go to **SSL/TLS** → **Overview**
2. Set encryption mode to **"Full"** (not "Full (strict)")
3. Go to **SSL/TLS** → **Edge Certificates**
4. Enable these settings:
   - ✅ **Always Use HTTPS**: ON
   - ✅ **Automatic HTTPS Rewrites**: ON
   - ✅ **Min TLS Version**: 1.2

### **Step 6: Optional Performance Settings**
1. Go to **Speed** → **Optimization**
2. Enable:
   - ✅ **Auto Minify**: CSS, JavaScript, HTML
   - ✅ **Brotli**: ON
3. Go to **Caching** → **Configuration**
4. Set **Browser Cache TTL**: 4 hours

### **Step 7: Test Your Setup**
Once nameservers are updated, test:

```bash
# Check DNS
nslookup checklist.stephentyrrell.ie

# Test HTTP (should redirect to HTTPS)
curl -I http://checklist.stephentyrrell.ie

# Test HTTPS (should work with trusted certificate)
curl -I https://checklist.stephentyrrell.ie

# Test health endpoint
curl https://checklist.stephentyrrell.ie/health
```

## 🔍 **Verification Checklist**

- [ ] Domain added to Cloudflare
- [ ] Nameservers updated at registrar
- [ ] DNS record pointing to Azure container
- [ ] SSL mode set to "Full"
- [ ] Always Use HTTPS enabled
- [ ] `https://checklist.stephentyrrell.ie` loads without warnings
- [ ] `http://checklist.stephentyrrell.ie` redirects to HTTPS

## 🐛 **Troubleshooting**

### DNS Not Propagating
```bash
# Check if nameservers updated
dig NS stephentyrrell.ie

# Should show Cloudflare nameservers
```

### SSL Errors
- Make sure SSL mode is "Full" (not "Full (strict)")
- Wait for SSL certificate provisioning (can take 15 minutes)

### Site Not Loading
1. Check if container is running:
   ```bash
   az container show --name checklist-generator --resource-group checklist-generator-rg
   ```
2. Check container logs:
   ```bash
   az container logs --name checklist-generator --resource-group checklist-generator-rg
   ```

## 🎉 **Expected Results**

After setup:
- **Primary URL**: https://checklist.stephentyrrell.ie
- **Trusted SSL**: ✅ Green lock in browser
- **Fast Loading**: Global CDN acceleration
- **Security**: DDoS protection
- **Analytics**: Available in Cloudflare dashboard

Your application will be production-ready with enterprise-level features, completely free! 🚀
