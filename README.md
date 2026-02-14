# RedLoki - Redis Distributed Lock Filter

A lightweight ASP.NET Core action filter that provides distributed locking using Redis to prevent concurrent execution of the same operation.

## Features

- 🔒 **Distributed Locking**: Prevent concurrent processing of requests with the same identifier
- ⚡ **Redis-Based**: Leverages Redis for fast, distributed lock management
- 🎯 **Flexible Key Resolution**: Supports multiple sources for lock keys (route, query, header, body)
- ⏱️ **Configurable Expiration**: Set custom lock expiration times
- 🚀 **Easy Integration**: Simple attribute-based usage

## Installation
Add Redis connection to your `Program.cs` or `Startup.cs`:
## Usage

### Basic Usage

Apply the `RedisLockFilter` to your controller action:
### Register the Filter

Register the filter in your DI container:
## Configuration

### Constructor Parameters
### Time Units

- `TimeUnit.Milliseconds`
- `TimeUnit.Seconds`
- `TimeUnit.Minutes` (default)
- `TimeUnit.Hours`
- `TimeUnit.Days`

## How It Works

1. **Key Resolution**: The filter searches for the lock key in this order:
   - Route data (`/api/orders/{orderId}`)
   - Action arguments (`[FromBody]`, `[FromQuery]`)
   - Query string (`?orderId=123`)
   - Request headers (`X-Request-Id`)
   - Properties of action arguments (nested properties)

2. **Lock Acquisition**: Attempts to acquire a Redis lock using `SET NX` command

3. **Request Handling**:
   - ✅ **Lock acquired**: Request proceeds
   - ❌ **Lock exists**: Returns `409 Conflict`
   - ❌ **Missing key**: Returns `400 Bad Request`

4. **Lock Expiration**: Locks automatically expire after the configured duration

## Response Codes

| Status Code | Description |
|-------------|-------------|
| `200 OK` | Request processed successfully |
| `400 Bad Request` | Lock key parameter is missing |
| `409 Conflict` | Resource is locked by another request |

## Best Practices

1. **Choose Appropriate Timeout**: Set lock expiration slightly longer than expected operation duration
2. **Use Meaningful Keys**: Select parameters that uniquely identify the operation
3. **Handle Conflicts**: Implement retry logic on the client side for `409` responses
4. **Monitor Redis**: Ensure Redis is highly available for production environments
5. **Consider Idempotency**: Use this filter for idempotent operations

## Common Use Cases

- 🛒 **E-commerce**: Prevent duplicate order submissions
- 💳 **Payments**: Avoid duplicate payment processing
- 📧 **Notifications**: Prevent duplicate email/SMS sends
- 🔄 **Webhooks**: Handle idempotent webhook processing
- 📊 **Data Processing**: Prevent concurrent processing of the same data

## Limitations

- Requires Redis server availability
- Lock is not automatically released on action completion (relies on expiration)
- Not suitable for long-running operations (use background jobs instead)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT License - feel free to use in your projects.

## Support

For issues and questions, please create an issue on the [GitHub repository](https://github.com/huseyinsimsekk/RedLoki).

---

**Made with ❤️ for distributed systems**
