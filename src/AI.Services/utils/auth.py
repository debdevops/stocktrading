import jwt
from datetime import datetime, timedelta
from typing import Dict, Any, Optional
import logging

logger = logging.getLogger(__name__)

def verify_jwt_token(token: str) -> Dict[str, Any]:
    """Verify JWT token and return payload"""
    try:
        # In production, use the same secret key as your ASP.NET Core services
        secret_key = "your-super-secret-key-that-is-at-least-32-characters-long-for-security"
        
        # Decode the token
        payload = jwt.decode(
            token,
            secret_key,
            algorithms=["HS256"],
            options={"verify_exp": True}
        )
        
        return payload
        
    except jwt.ExpiredSignatureError:
        logger.error("Token has expired")
        raise Exception("Token has expired")
    except jwt.InvalidTokenError as e:
        logger.error(f"Invalid token: {str(e)}")
        raise Exception("Invalid token")
    except Exception as e:
        logger.error(f"Token verification failed: {str(e)}")
        raise Exception("Token verification failed")

def create_jwt_token(user_id: str, email: str, roles: list = None) -> str:
    """Create JWT token for testing purposes"""
    try:
        secret_key = "your-super-secret-key-that-is-at-least-32-characters-long-for-security"
        
        payload = {
            "user_id": user_id,
            "email": email,
            "roles": roles or ["User"],
            "iat": datetime.utcnow(),
            "exp": datetime.utcnow() + timedelta(hours=1),
            "iss": "StockTradingAPI",
            "aud": "StockTradingClient"
        }
        
        token = jwt.encode(payload, secret_key, algorithm="HS256")
        return token
        
    except Exception as e:
        logger.error(f"Token creation failed: {str(e)}")
        raise Exception("Token creation failed")
