import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../../lib/api';
import type { UserProfile } from '../../lib/api';
import { useAuth } from '../../context/AuthContext';

interface UserManagementProps {
    token: string | null;
}

const UserManagement: React.FC<UserManagementProps> = ({ token }) => {
    const { t } = useTranslation();
    const [users, setUsers] = useState<UserProfile[]>([]);
    const [roles, setRoles] = useState<string[]>([]);
    const [loading, setLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    const fetchUsersAndRoles = () => {
        if (!token) return;
        setLoading(true);
        setError(null);
        Promise.all([api.getAllUsers(token), api.getAllRoles(token)])
            .then(([userRes, roleRes]) => {
                setUsers(userRes);
                setRoles(roleRes);
            })
            .catch((err) => setError(err.message || t('users_and_roles_error')))
            .finally(() => setLoading(false));
    };

    useEffect(() => {
        fetchUsersAndRoles();
    }, [token]);

    return (
        <section className="user-management panel">
            <h3>👥 {t('user_management_title')}</h3>
            {error && <p className="error">⚠️ {error}</p>}
            {loading && <p>⏳ {t('loading')}</p>}
            {!loading && (
                <div className="user-list">
                    <table>
                        <thead>
                            <tr>
                                <th>👤 {t('user')}</th>
                                <th>🔐 {t('roles')}</th>
                                <th>⚙️ {t('actions')}</th>
                            </tr>
                        </thead>
                        <tbody>
                            {users.map((u) => (
                                <UserRow
                                    key={u.id}
                                    user={u}
                                    allRoles={roles}
                                    onRoleChange={fetchUsersAndRoles}
                                />
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </section>
    );
};

interface UserRowProps {
    user: UserProfile;
    allRoles: string[];
    onRoleChange: () => void;
}

const UserRow: React.FC<UserRowProps> = ({ user, allRoles, onRoleChange }) => {
    const { token } = useAuth();
    const { t } = useTranslation();
    const [selectedRole, setSelectedRole] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [isEditing, setIsEditing] = useState(false);

    const availableRoles = allRoles.filter((r) => !user.roles?.includes(r));

    useEffect(() => {
        if (availableRoles.length > 0) {
            setSelectedRole(availableRoles[0]);
        }
    }, [user]);

    const handleAssignRole = async () => {
        if (!token || !selectedRole || !user.id) return;
        setError(null);
        try {
            await api.assignRole({ userId: user.id, roleName: selectedRole }, token);
            onRoleChange();
        } catch (err: any) {
            setError(err.message);
        }
    };

    const handleRemoveRole = async (roleName: string) => {
        if (!token || !user.id) return;
        setError(null);
        try {
            await api.removeRole({ userId: user.id, roleName: roleName }, token);
            onRoleChange();
        } catch (err: any) {
            setError(err.message);
        }
    };

    return (
        <tr>
            <td>
                <div className="user-info">
                    <span>
                        {user.firstName} {user.lastName}
                    </span>
                    <small>{user.email}</small>
                </div>
            </td>
            <td>
                <div className="user-roles">
                    {user.roles?.map((role) => (
                        <span key={role} className="role-badge">
                            {role}
                            <button onClick={() => handleRemoveRole(role)}>×</button>
                        </span>
                    ))}
                </div>
            </td>
            <td>
                <div className="role-actions">
                    {isEditing ? (
                        <>
                            <select
                                value={selectedRole}
                                onChange={(e) => setSelectedRole(e.target.value)}
                                disabled={availableRoles.length === 0}
                            >
                                {availableRoles.map((r) => (
                                    <option key={r} value={r}>
                                        {r}
                                    </option>
                                ))}
                            </select>
                            <button onClick={handleAssignRole} disabled={availableRoles.length === 0}>
                                {t('add_role')}
                            </button>
                            <button onClick={() => setIsEditing(false)} className="cancel-btn">
                                {t('cancel')}
                            </button>
                        </>
                    ) : (
                        <button className="edit-role-btn" onClick={() => setIsEditing(true)}>
                            ✏️ {t('edit')}
                        </button>
                    )}
                </div>
                {error && <small className="error-message">{error}</small>}
            </td>
        </tr>
    );
};

export default UserManagement;
